# NAXAPA — Backup e restauração do NaxapaDB

## Backup automatizado

Abra PowerShell como Administrador e execute:

`./scripts/Backup-NaxapaDB.ps1`

O script cria backup com `CHECKSUM` e depois executa `RESTORE VERIFYONLY`.

## Backup manual no SSMS

```sql
BACKUP DATABASE NaxapaDB
TO DISK = 'C:\\NAXAPA\\Backup\\NaxapaDB.bak'
WITH COPY_ONLY, CHECKSUM, INIT, STATS = 10;
GO

RESTORE VERIFYONLY
FROM DISK = 'C:\\NAXAPA\\Backup\\NaxapaDB.bak'
WITH CHECKSUM;
GO
```

## Restauração

Antes de restaurar:

1. pare a Gestão/API, PDV e Print Agent;
2. preserve uma cópia do banco atual;
3. confirme o arquivo `.bak` com `RESTORE VERIFYONLY`;
4. execute em janela de manutenção.

Exemplo:

```sql
USE master;
GO
ALTER DATABASE NaxapaDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
GO
RESTORE DATABASE NaxapaDB
FROM DISK = 'C:\\NAXAPA\\Backup\\NaxapaDB.bak'
WITH REPLACE, CHECKSUM, STATS = 10;
GO
ALTER DATABASE NaxapaDB SET MULTI_USER;
GO
```

Depois execute `tests/ValidateDatabase.sql` e valide login, produtos, pedidos e caixa antes de liberar os operadores.

Nunca restaure por cima do banco sem backup prévio e janela controlada.
