using System;
using System.Globalization;
using System.IO;
using Microsoft.Data.SqlClient;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace CoworkingApp
{
    /// <summary>
    /// Gera um recibo PDF para um pagamento. Lê os dados completos a partir
    /// da BD (cliente, snapshot de preço, serviço associado) e escreve um
    /// PDF A4 simples mas profissional. Usa PdfSharp 6.x.
    /// </summary>
    public static class ReciboPdf
    {
        public class PagamentoData
        {
            public int PagamentoId;
            public DateTime DataPagamento;
            public decimal Valor;
            public decimal PrecoServicoSnapshot;
            public string Metodo;
            public string Estado;

            public string ClienteNome;
            public string ClienteNif;
            public string ClienteEmail;
            public string ClienteTelefone;

            public string ServicoTipo;       // "Adesão" ou "Reserva"
            public string ServicoDescricao;  // ex: "Plano Flex" ou "Sala A1 (15/05/2026 09:00-11:00)"
            public DateTime? ServicoData;
        }

        public static PagamentoData Fetch(int pagamentoId)
        {
            const string sql = @"
                SELECT pg.pagamento_id, pg.data_pagamento, pg.valor,
                       pg.preco_servico_snapshot, pg.metodo_pagamento, pg.estado,
                       pg.adesao_id, pg.reserva_id,
                       c.nome AS cli_nome, c.nif AS cli_nif,
                       c.email AS cli_email, c.telefone AS cli_tel,
                       pl.nome_plano AS plano_nome,
                       r.data_reserva AS r_data, r.hora_inicio AS r_hi, r.hora_fim AS r_hf,
                       CASE WHEN s.recurso_id IS NOT NULL THEN 'Sala ' + s.nome
                            WHEN p.recurso_id IS NOT NULL THEN 'Posto ' + p.codigo
                            ELSE NULL END AS r_recurso
                FROM pagamento pg
                JOIN cliente c     ON pg.cliente_id  = c.cliente_id
                LEFT JOIN adesao a ON pg.adesao_id   = a.adesao_id
                LEFT JOIN plano pl ON a.plano_id    = pl.plano_id
                LEFT JOIN reserva r ON pg.reserva_id = r.reserva_id
                LEFT JOIN recurso rc ON r.recurso_id = rc.recurso_id
                LEFT JOIN sala s   ON rc.recurso_id = s.recurso_id
                LEFT JOIN posto p  ON rc.recurso_id = p.recurso_id
                WHERE pg.pagamento_id = @id";

            using (var conn = Database.GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@id", pagamentoId);
                using (var rd = cmd.ExecuteReader())
                {
                    if (!rd.Read()) return null;
                    var d = new PagamentoData
                    {
                        PagamentoId          = Convert.ToInt32(rd["pagamento_id"]),
                        DataPagamento        = Convert.ToDateTime(rd["data_pagamento"]),
                        Valor                = Convert.ToDecimal(rd["valor"]),
                        PrecoServicoSnapshot = Convert.ToDecimal(rd["preco_servico_snapshot"]),
                        Metodo               = rd["metodo_pagamento"].ToString(),
                        Estado               = rd["estado"].ToString(),
                        ClienteNome          = rd["cli_nome"].ToString(),
                        ClienteNif           = rd["cli_nif"].ToString(),
                        ClienteEmail         = rd["cli_email"] is DBNull ? "" : rd["cli_email"].ToString(),
                        ClienteTelefone      = rd["cli_tel"]   is DBNull ? "" : rd["cli_tel"].ToString(),
                    };

                    if (rd["adesao_id"] != DBNull.Value)
                    {
                        d.ServicoTipo = "Adesão #" + Convert.ToInt32(rd["adesao_id"]);
                        d.ServicoDescricao = rd["plano_nome"] is DBNull ? "Plano" : ("Plano " + rd["plano_nome"]);
                    }
                    else if (rd["reserva_id"] != DBNull.Value)
                    {
                        d.ServicoTipo = "Reserva #" + Convert.ToInt32(rd["reserva_id"]);
                        d.ServicoData = rd["r_data"] is DBNull ? (DateTime?)null : Convert.ToDateTime(rd["r_data"]);
                        string recurso = rd["r_recurso"] is DBNull ? "Recurso" : rd["r_recurso"].ToString();
                        string horas = "";
                        if (rd["r_hi"] != DBNull.Value && rd["r_hf"] != DBNull.Value)
                        {
                            var hi = (TimeSpan)rd["r_hi"];
                            var hf = (TimeSpan)rd["r_hf"];
                            horas = $" ({hi:hh\\:mm}-{hf:hh\\:mm})";
                        }
                        else
                        {
                            horas = " (dia completo)";
                        }
                        d.ServicoDescricao = recurso + horas;
                    }
                    return d;
                }
            }
        }

        public static void Generate(PagamentoData d, string outputPath)
        {
            var doc = new PdfDocument();
            doc.Info.Title    = $"Recibo #{d.PagamentoId:D6}";
            doc.Info.Author   = "Coworking — Sistema de Gestão";
            doc.Info.Subject  = "Recibo de pagamento";
            doc.Info.Creator  = "CoworkingApp";

            var page = doc.AddPage();
            page.Size = PdfSharp.PageSize.A4;
            var g = XGraphics.FromPdfPage(page);

            double W = page.Width.Point;
            double H = page.Height.Point;
            double margin = 50;

            var fontTitle    = new XFont("Helvetica", 22, XFontStyleEx.Bold);
            var fontH1       = new XFont("Helvetica", 14, XFontStyleEx.Bold);
            var fontH2       = new XFont("Helvetica", 10, XFontStyleEx.Bold);
            var fontBody     = new XFont("Helvetica", 11, XFontStyleEx.Regular);
            var fontSmall    = new XFont("Helvetica", 9,  XFontStyleEx.Regular);
            var fontMicro    = new XFont("Helvetica", 8,  XFontStyleEx.Regular);

            var indigo = XColor.FromArgb(99, 102, 241);
            var slate  = XColor.FromArgb(51, 65, 85);
            var muted  = XColor.FromArgb(148, 163, 184);
            var border = XColor.FromArgb(226, 232, 240);

            // ── Header ────────────────────────────────────────────────────
            // Top accent stripe
            g.DrawRectangle(new XSolidBrush(indigo), 0, 0, W, 6);

            double y = margin;
            g.DrawString("COWORKING", fontH1, new XSolidBrush(indigo),
                new XRect(margin, y, 200, 20), XStringFormats.TopLeft);
            g.DrawString("Sistema de Gestão", fontSmall, new XSolidBrush(muted),
                new XRect(margin, y + 22, 200, 14), XStringFormats.TopLeft);

            g.DrawString("RECIBO", fontTitle, new XSolidBrush(slate),
                new XRect(W - margin - 200, y, 200, 28), XStringFormats.TopRight);
            g.DrawString($"Nº {d.PagamentoId:D6}", fontH2, new XSolidBrush(muted),
                new XRect(W - margin - 200, y + 32, 200, 14), XStringFormats.TopRight);

            y += 70;
            // Linha separadora
            g.DrawLine(new XPen(border, 1), margin, y, W - margin, y);

            // Data emissão (canto direito sob a linha)
            y += 10;
            g.DrawString($"Data de emissão: {DateTime.Now:dd/MM/yyyy HH:mm}",
                fontSmall, new XSolidBrush(muted),
                new XRect(margin, y, W - 2 * margin, 14), XStringFormats.TopRight);
            y += 26;

            // ── Cliente ──────────────────────────────────────────────────
            y = DrawSection(g, "CLIENTE", margin, y, W - 2 * margin, fontH2, indigo, border);
            y = DrawField(g, "Nome",     d.ClienteNome,                       margin, y, fontH2, fontBody, slate, muted);
            y = DrawField(g, "NIF",      d.ClienteNif,                        margin, y, fontH2, fontBody, slate, muted);
            if (!string.IsNullOrWhiteSpace(d.ClienteEmail))
                y = DrawField(g, "Email", d.ClienteEmail,                      margin, y, fontH2, fontBody, slate, muted);
            if (!string.IsNullOrWhiteSpace(d.ClienteTelefone))
                y = DrawField(g, "Telefone", d.ClienteTelefone,                margin, y, fontH2, fontBody, slate, muted);

            y += 16;

            // ── Serviço ──────────────────────────────────────────────────
            y = DrawSection(g, "SERVIÇO", margin, y, W - 2 * margin, fontH2, indigo, border);
            y = DrawField(g, "Tipo",        d.ServicoTipo ?? "—",             margin, y, fontH2, fontBody, slate, muted);
            y = DrawField(g, "Descrição",   d.ServicoDescricao ?? "—",        margin, y, fontH2, fontBody, slate, muted);
            if (d.ServicoData.HasValue)
                y = DrawField(g, "Data do serviço", d.ServicoData.Value.ToString("dd/MM/yyyy"),
                                                                              margin, y, fontH2, fontBody, slate, muted);

            y += 16;

            // ── Pagamento ────────────────────────────────────────────────
            y = DrawSection(g, "PAGAMENTO", margin, y, W - 2 * margin, fontH2, indigo, border);
            y = DrawField(g, "Data",          d.DataPagamento.ToString("dd/MM/yyyy"), margin, y, fontH2, fontBody, slate, muted);
            y = DrawField(g, "Método",        d.Metodo,                                margin, y, fontH2, fontBody, slate, muted);
            y = DrawField(g, "Estado",        d.Estado,                                margin, y, fontH2, fontBody, slate, muted);
            y = DrawField(g, "Preço do serviço (snapshot)",
                FormatEuro(d.PrecoServicoSnapshot),                                     margin, y, fontH2, fontBody, slate, muted);

            y += 12;

            // ── Caixa "Total pago" ───────────────────────────────────────
            double boxH = 60;
            double boxW = W - 2 * margin;
            var boxRect = new XRect(margin, y, boxW, boxH);
            g.DrawRoundedRectangle(new XPen(indigo, 1.5), new XSolidBrush(XColor.FromArgb(238, 242, 255)),
                boxRect, new XSize(10, 10));
            g.DrawString("TOTAL PAGO", fontH2, new XSolidBrush(slate),
                new XRect(margin + 16, y + 12, 200, 16), XStringFormats.TopLeft);
            var totalFont = new XFont("Helvetica", 22, XFontStyleEx.Bold);
            g.DrawString(FormatEuro(d.Valor), totalFont, new XSolidBrush(indigo),
                new XRect(margin, y + 16, boxW - 20, 26), XStringFormats.TopRight);

            y += boxH + 16;

            // ── Footer ───────────────────────────────────────────────────
            g.DrawLine(new XPen(border, 1), margin, H - margin - 30, W - margin, H - margin - 30);
            g.DrawString("Documento gerado automaticamente. Guarde como prova de pagamento.",
                fontMicro, new XSolidBrush(muted),
                new XRect(margin, H - margin - 22, W - 2 * margin, 12), XStringFormats.TopLeft);
            g.DrawString($"Gerado em {DateTime.Now:dd/MM/yyyy HH:mm:ss}",
                fontMicro, new XSolidBrush(muted),
                new XRect(margin, H - margin - 22, W - 2 * margin, 12), XStringFormats.TopRight);

            doc.Save(outputPath);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static double DrawSection(XGraphics g, string title, double x, double y, double width,
                                          XFont font, XColor accent, XColor border)
        {
            g.DrawString(title, font, new XSolidBrush(accent),
                new XRect(x, y, width, 14), XStringFormats.TopLeft);
            g.DrawLine(new XPen(border, 0.5), x, y + 18, x + width, y + 18);
            return y + 26;
        }

        private static double DrawField(XGraphics g, string label, string value,
                                         double x, double y, XFont fontLabel, XFont fontValue,
                                         XColor colValue, XColor colLabel)
        {
            // Layout 2 colunas: label esquerda 160, value 380
            g.DrawString(label, fontLabel, new XSolidBrush(colLabel),
                new XRect(x, y, 160, 14), XStringFormats.TopLeft);
            g.DrawString(value ?? "—", fontValue, new XSolidBrush(colValue),
                new XRect(x + 170, y - 2, 380, 16), XStringFormats.TopLeft);
            return y + 22;
        }

        private static string FormatEuro(decimal v)
        {
            return v.ToString("N2", new CultureInfo("pt-PT")) + " €";
        }

        /// <summary>Cria nome de ficheiro sugerido + abre SaveFileDialog ao
        /// chamador. Retorna o path escolhido, ou null se cancelado.</summary>
        public static string SuggestFilename(PagamentoData d)
        {
            string cliSafe = string.Concat((d.ClienteNome ?? "cliente").Split(Path.GetInvalidFileNameChars()));
            cliSafe = cliSafe.Replace(' ', '_');
            if (cliSafe.Length > 40) cliSafe = cliSafe.Substring(0, 40);
            return $"Recibo_{d.PagamentoId:D6}_{cliSafe}.pdf";
        }
    }
}
