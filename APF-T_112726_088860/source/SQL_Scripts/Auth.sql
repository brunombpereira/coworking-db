-- =====================================================================
-- Auth — registo e login de utilizadores
-- Hash: SHA2_256 com salt aleatório de 16 bytes.
-- =====================================================================
USE CoworkingDB;
GO

-- Registo de novo utilizador -----------------------------------------
-- Para role 'Cliente' tem de existir um cliente_id válido (1:1 com tabela cliente).
-- Para 'Admin'/'Staff' o cliente_id deve ser NULL (validado por CHECK).
CREATE OR ALTER PROCEDURE sp_register_user
    @username       NVARCHAR(100),
    @password       NVARCHAR(255),
    @role           NVARCHAR(20),
    @cliente_id     INTEGER       = NULL,
    @utilizador_id  INTEGER       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM utilizador WHERE username = @username)
    BEGIN
        THROW 52001, 'Username já existe.', 1;
    END

    DECLARE @salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
    DECLARE @hash VARBINARY(64) =
        HASHBYTES('SHA2_256', CAST(@salt AS VARBINARY(16))
                            + CAST(@password AS VARBINARY(1024)));

    INSERT INTO utilizador (username, password_hash, salt, role, cliente_id)
    VALUES (@username, @hash, @salt, @role, @cliente_id);

    SET @utilizador_id = SCOPE_IDENTITY();
END;
GO

-- Login --------------------------------------------------------------
-- Retorna utilizador_id, role e cliente_id se as credenciais forem
-- válidas e o utilizador estiver ativo. Atualiza ultimo_login.
-- Em caso de credenciais inválidas devolve resultset vazio (não dá
-- pista se o username existe ou não).
CREATE OR ALTER PROCEDURE sp_login_user
    @username NVARCHAR(100),
    @password NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @utilizador_id INTEGER,
            @salt          VARBINARY(16),
            @stored_hash   VARBINARY(64),
            @role          NVARCHAR(20),
            @cliente_id    INTEGER,
            @ativo         BIT;

    SELECT @utilizador_id = utilizador_id,
           @salt          = salt,
           @stored_hash   = password_hash,
           @role          = role,
           @cliente_id    = cliente_id,
           @ativo         = ativo
    FROM   utilizador
    WHERE  username = @username;

    IF @utilizador_id IS NULL OR @ativo = 0
    BEGIN
        SELECT NULL AS utilizador_id, NULL AS role, NULL AS cliente_id WHERE 1 = 0;
        RETURN;
    END

    DECLARE @candidate_hash VARBINARY(64) =
        HASHBYTES('SHA2_256', CAST(@salt AS VARBINARY(16))
                            + CAST(@password AS VARBINARY(1024)));

    IF @candidate_hash <> @stored_hash
    BEGIN
        SELECT NULL AS utilizador_id, NULL AS role, NULL AS cliente_id WHERE 1 = 0;
        RETURN;
    END

    UPDATE utilizador SET ultimo_login = SYSDATETIME() WHERE utilizador_id = @utilizador_id;

    SELECT @utilizador_id AS utilizador_id,
           @role          AS role,
           @cliente_id    AS cliente_id;
END;
GO

-- Alterar password ---------------------------------------------------
CREATE OR ALTER PROCEDURE sp_change_password
    @utilizador_id   INTEGER,
    @password_atual  NVARCHAR(255),
    @password_nova   NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @salt VARBINARY(16), @stored_hash VARBINARY(64);
    SELECT @salt = salt, @stored_hash = password_hash
    FROM   utilizador
    WHERE  utilizador_id = @utilizador_id;

    IF @salt IS NULL
    BEGIN
        THROW 52002, 'Utilizador não encontrado.', 1;
    END

    IF HASHBYTES('SHA2_256', CAST(@salt AS VARBINARY(16))
                           + CAST(@password_atual AS VARBINARY(1024))) <> @stored_hash
    BEGIN
        THROW 52003, 'Password atual incorreta.', 1;
    END

    DECLARE @new_salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
    DECLARE @new_hash VARBINARY(64) =
        HASHBYTES('SHA2_256', CAST(@new_salt AS VARBINARY(16))
                            + CAST(@password_nova AS VARBINARY(1024)));

    UPDATE utilizador
    SET    password_hash = @new_hash,
           salt          = @new_salt
    WHERE  utilizador_id = @utilizador_id;
END;
GO

-- Desativar utilizador (soft-delete) ---------------------------------
CREATE OR ALTER PROCEDURE sp_desativar_utilizador
    @utilizador_id INTEGER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE utilizador SET ativo = 0 WHERE utilizador_id = @utilizador_id;
END;
GO

-- =====================================================================
-- Administração de utilizadores (só app_admin invoca)
-- =====================================================================

-- Criar utilizador qualquer (Admin/Staff/Cliente) ---------------------
-- Para role=Cliente o cliente_id tem de existir já. Para Admin/Staff,
-- cliente_id deve ser NULL — a CHECK ck_utilizador_cliente_role enforça.
CREATE OR ALTER PROCEDURE sp_admin_create_user
    @username      NVARCHAR(100),
    @password      NVARCHAR(255),
    @role          NVARCHAR(20),
    @cliente_id    INTEGER       = NULL,
    @utilizador_id INTEGER       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @role NOT IN ('Admin', 'Staff', 'Cliente')
        THROW 52010, 'Role inválida — usar Admin, Staff ou Cliente.', 1;

    IF LEN(@password) < 8
        THROW 52011, 'A password deve ter pelo menos 8 caracteres.', 1;

    EXEC sp_register_user
         @username      = @username,
         @password      = @password,
         @role          = @role,
         @cliente_id    = @cliente_id,
         @utilizador_id = @utilizador_id OUTPUT;
END;
GO

-- Reset de password sem exigir a antiga ------------------------------
-- Usado pelo admin para destrancar uma conta. O admin é responsável
-- por comunicar a nova password ao utilizador por canal seguro.
CREATE OR ALTER PROCEDURE sp_admin_reset_password
    @utilizador_id INTEGER,
    @password_nova NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM utilizador WHERE utilizador_id = @utilizador_id)
        THROW 52012, 'Utilizador inexistente.', 1;

    IF LEN(@password_nova) < 8
        THROW 52013, 'A nova password deve ter pelo menos 8 caracteres.', 1;

    DECLARE @new_salt VARBINARY(16) = CRYPT_GEN_RANDOM(16);
    DECLARE @new_hash VARBINARY(64) =
        HASHBYTES('SHA2_256', CAST(@new_salt AS VARBINARY(16))
                            + CAST(@password_nova AS VARBINARY(1024)));

    UPDATE utilizador
    SET    password_hash = @new_hash,
           salt          = @new_salt
    WHERE  utilizador_id = @utilizador_id;
END;
GO

-- Activar / desactivar conta -----------------------------------------
CREATE OR ALTER PROCEDURE sp_admin_toggle_user_active
    @utilizador_id INTEGER,
    @ativo         BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM utilizador WHERE utilizador_id = @utilizador_id)
        THROW 52014, 'Utilizador inexistente.', 1;

    UPDATE utilizador SET ativo = @ativo WHERE utilizador_id = @utilizador_id;
END;
GO

-- View para listagem na UI -------------------------------------------
CREATE OR ALTER VIEW vw_utilizadores_listagem
AS
SELECT
    u.utilizador_id,
    u.username,
    u.role,
    u.ativo,
    u.cliente_id,
    c.nome              AS cliente_nome,
    u.data_criacao,
    u.ultimo_login
FROM utilizador u
LEFT JOIN cliente c ON u.cliente_id = c.cliente_id;
GO
