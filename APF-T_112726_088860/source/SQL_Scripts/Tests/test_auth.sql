-- =====================================================================
-- test_auth.sql — registo, login, mudança de password
-- =====================================================================
USE CoworkingDB;
GO

SET NOCOUNT ON;
PRINT N'--- TC12: registo duplicado deve falhar (52001) ---';
DECLARE @uid INT;
EXEC sp_register_user N'staff_teste', N'Password1!', 'Staff', NULL, @uid OUTPUT;
PRINT CONCAT(N'Criado utilizador_id=', @uid);

BEGIN TRY
    EXEC sp_register_user N'staff_teste', N'Outra1!', 'Staff', NULL, @uid OUTPUT;
    PRINT N'FALHA: deveria ter lançado 52001';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK: capturado ', ERROR_NUMBER(), N' ', ERROR_MESSAGE());
END CATCH
GO

PRINT N'--- TC13: login com password errada -> resultset vazio ---';
EXEC sp_login_user N'staff_teste', N'errada';
GO

PRINT N'--- TC14: login válido -> resultset com utilizador ---';
EXEC sp_login_user N'staff_teste', N'Password1!';
GO

PRINT N'--- TC15: alterar password com atual errada (52003) ---';
DECLARE @uid INT = (SELECT utilizador_id FROM utilizador WHERE username = N'staff_teste');
BEGIN TRY
    EXEC sp_change_password @uid, N'errada', N'NovaPwd!';
    PRINT N'FALHA: deveria ter lançado 52003';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK: capturado ', ERROR_NUMBER(), N' ', ERROR_MESSAGE());
END CATCH
GO

-- cleanup
DELETE FROM utilizador WHERE username = N'staff_teste';
GO
