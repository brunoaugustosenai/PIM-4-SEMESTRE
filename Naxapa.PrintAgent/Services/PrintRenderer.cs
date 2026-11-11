using System.Globalization;
using System.Text;
using System.Text.Json;
namespace Naxapa.PrintAgent.Services;
public static class PrintRenderer
{
    public static byte[] Render(string payloadJson,int paperWidth=48)
    {
        using var doc=JsonDocument.Parse(payloadJson);var r=doc.RootElement;var sb=new StringBuilder();
        sb.AppendLine(Center("NAXAPA",paperWidth));sb.AppendLine(new string('-',Math.Min(paperWidth,48)));
        var type=Get(r,"tipo")??"DOCUMENTO";
        if(type=="TESTE") { sb.AppendLine(Center(Get(r,"titulo")??"Teste de impressao",paperWidth));sb.AppendLine(Get(r,"impressora")??"");sb.AppendLine(Get(r,"mensagem")??""); }
        else if(type=="PRODUCAO") { sb.AppendLine($"PEDIDO #{GetNumber(r,"numero")}");sb.AppendLine($"ATENDIMENTO: {Get(r,"atendimento")}");sb.AppendLine(new string('-',Math.Min(paperWidth,48)));if(r.TryGetProperty("itens",out var items)){foreach(var item in items.EnumerateArray()){sb.AppendLine($"{GetDecimal(item,"quantidade"):0.###}x {Get(item,"produto")}");if(item.TryGetProperty("opcoes",out var opts)){foreach(var o in opts.EnumerateArray())sb.AppendLine($"  + {GetNumber(o,"quantidade")}x {Get(o,"nome")}");}var obs=Get(item,"observacao");if(!string.IsNullOrWhiteSpace(obs))sb.AppendLine($"  OBS: {obs}");}}var orderObs=Get(r,"observacao");if(!string.IsNullOrWhiteSpace(orderObs))sb.AppendLine($"OBS PEDIDO: {orderObs}");}
        else if(type=="COMPROVANTE")
        {
            if(r.TryGetProperty("estabelecimento",out var est))
            {
                sb.AppendLine(Center(Get(est,"nome")??"NAXAPA",paperWidth));
                var docValue=Get(est,"documento");if(!string.IsNullOrWhiteSpace(docValue))sb.AppendLine($"CNPJ/CPF: {docValue}");
                var phone=Get(est,"telefone");if(!string.IsNullOrWhiteSpace(phone))sb.AppendLine($"TEL: {phone}");
            }
            sb.AppendLine($"PEDIDO #{GetNumber(r,"numero")}");
            sb.AppendLine($"ATENDIMENTO: {Get(r,"atendimento")}");
            sb.AppendLine($"OPERADOR: {Get(r,"operador")}");
            sb.AppendLine(new string('-',Math.Min(paperWidth,48)));
            if(r.TryGetProperty("itens",out var receiptItems))
            {
                foreach(var item in receiptItems.EnumerateArray())
                {
                    var qty=GetDecimal(item,"quantidade");var name=Get(item,"produto");var total=GetDecimal(item,"total");
                    sb.AppendLine($"{qty:0.###}x {name}");sb.AppendLine($"  {total:C}");
                    if(item.TryGetProperty("opcoes",out var opts)){foreach(var o in opts.EnumerateArray())sb.AppendLine($"   + {GetNumber(o,"quantidade")}x {Get(o,"nome")} {GetDecimal(o,"total"):C}");}
                }
            }
            sb.AppendLine(new string('-',Math.Min(paperWidth,48)));
            sb.AppendLine($"SUBTOTAL: {GetDecimal(r,"subtotal"):C}");
            var discount=GetDecimal(r,"desconto");if(discount>0)sb.AppendLine($"DESCONTO: -{discount:C}");
            var addition=GetDecimal(r,"acrescimo");if(addition>0)sb.AppendLine($"ACRESCIMO: {addition:C}");
            sb.AppendLine($"TOTAL: {GetDecimal(r,"total"):C}");
            if(r.TryGetProperty("pagamentos",out var payments)){sb.AppendLine("PAGAMENTOS:");foreach(var payment in payments.EnumerateArray()){sb.AppendLine($"  {Get(payment,"forma")}: {GetDecimal(payment,"valor"):C}");var change=GetDecimal(payment,"troco");if(change>0)sb.AppendLine($"  TROCO: {change:C}");}}
            sb.AppendLine(new string('-',Math.Min(paperWidth,48)));
            sb.AppendLine(Center("DOCUMENTO NAO FISCAL",paperWidth));
        }
        else { sb.AppendLine(payloadJson); }
        sb.AppendLine(new string('-',Math.Min(paperWidth,48)));sb.AppendLine(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));sb.AppendLine();sb.AppendLine();sb.AppendLine();
        var text=NormalizeForThermal(sb.ToString());return Encoding.ASCII.GetBytes("\x1B\x40"+text+"\n\n\x1D\x56\x00");
    }
    private static string? Get(JsonElement e,string n)=>e.TryGetProperty(n,out var p)&&p.ValueKind!=JsonValueKind.Null?p.ToString():null;
    private static long GetNumber(JsonElement e,string n)=>e.TryGetProperty(n,out var p)&&p.TryGetInt64(out var v)?v:0;
    private static decimal GetDecimal(JsonElement e,string n)=>e.TryGetProperty(n,out var p)&&p.TryGetDecimal(out var v)?v:0;
    private static string Center(string value,int width)=>value.Length>=width?value:new string(' ',Math.Max(0,(width-value.Length)/2))+value;
    private static string NormalizeForThermal(string value)
    {
        var decomposed=value.Normalize(NormalizationForm.FormD);
        var chars=decomposed.Where(c=>CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)
            .Where(c=>c=='\n'||c=='\r'||c=='\t'||(!char.IsControl(c)&&c<=127)).ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC);
    }
}
