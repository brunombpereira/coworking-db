# Screenshots — APF-T

Esta pasta deve conter screenshots da aplicação em modo light e dark, para o relatório APF-T.

## Como capturar

1. Lança a app: `APF-T_112726_088860/source/CoworkingApp/bin/Debug/net48/CoworkingApp.exe`
2. Por cada UserControl da sidebar (Dashboard, Clientes, Planos, Espaços/Salas/Postos, Reservas, Adesões, Pagamentos, Relatórios), tira screenshot com `Win+Shift+S` e guarda como `light-<nome>.png`.
3. Toggle modo escuro pelo botão na base da sidebar e repete com prefixo `dark-`.
4. Para os modais, abrir Novo/Editar e capturar com prefixo `<modo>-<nome>-modal.png`.

## Lista esperada

### Light mode
- light-dashboard.png
- light-clientes.png
- light-clientes-modal.png
- light-planos.png
- light-espacos-salas.png
- light-espacos-postos.png
- light-reservas.png
- light-reservas-modal-sala.png
- light-reservas-modal-posto.png
- light-adesoes.png
- light-adesoes-modal-fixo.png
- light-pagamentos.png
- light-pagamentos-modal.png
- light-relatorios-1.png (cada tab)

### Dark mode
- Espelhar tudo com prefixo `dark-`.

## Validação

- Toggle light↔dark deve mudar instantaneamente nos novos UserControls criados após o toggle (limitação conhecida: UCs já abertos não são re-pintados em runtime).
- Persistência: definir Dark, fechar app, reabrir → abre em Dark.
- Charts no Dashboard e Relatórios devem usar paleta indigo (#6366f1, #8b5cf6, #10b981, #f59e0b, #ef4444).
- Todos os modais centram com overlay escurecido. Esc cancela.
