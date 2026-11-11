namespace Naxapa.PrintAgent.Models;
public sealed record TokenResponse(string Token,string TokenType,DateTime ExpiresAtUtc,long UserId,string UserName,long EstabelecimentoId,string Estabelecimento);
public sealed record PrinterDto(long Id,string Name,string? Model,string ConnectionType,string? IpAddress,int? Port,string? WindowsName,int? PaperWidth,bool Default,bool Active);
public sealed record PrintJobDto(long Id,long? OrderId,long PrinterId,long? ProductionSectorId,string DocumentType,string Status,int Attempts,Guid IdempotencyKey,string PayloadJson,DateTime RequestedAtUtc,string? LastError);
