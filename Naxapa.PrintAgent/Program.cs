using Naxapa.PrintAgent.Services;
Console.Title="NAXAPA Print Agent";
Console.WriteLine("NAXAPA Print Agent (.NET 10)\n");
Console.Write("API [http://localhost:5180]: ");var baseUrl=Console.ReadLine();if(string.IsNullOrWhiteSpace(baseUrl))baseUrl="http://localhost:5180";
Console.Write("Usuario/e-mail: ");var login=Console.ReadLine()??"";
Console.Write("Senha: ");var password=ReadPassword();
Console.Write("ID estabelecimento (opcional): ");long? establishmentId=long.TryParse(Console.ReadLine(),out var est)?est:null;
using var cts=new CancellationTokenSource();Console.CancelKeyPress+=(s,e)=>{e.Cancel=true;cts.Cancel();};
try
{
    var api=new ApiClient(baseUrl);var session=await api.LoginAsync(login,password,establishmentId,cts.Token);Console.WriteLine($"\nConectado: {session.UserName} - {session.Estabelecimento}");
    while(!cts.IsCancellationRequested)
    {
        try
        {
            var printers=(await api.PrintersAsync(cts.Token)).ToDictionary(x=>x.Id);var jobs=await api.PendingAsync(cts.Token);
            foreach(var job in jobs)
            {
                if(!printers.TryGetValue(job.PrinterId,out var printer)){Console.WriteLine($"Job {job.Id}: impressora {job.PrinterId} nao encontrada.");continue;}
                try{await api.ClaimAsync(job.Id,cts.Token);var bytes=PrintRenderer.Render(job.PayloadJson,printer.PaperWidth??48);var copies=ReadCopies(job.PayloadJson);for(var i=0;i<copies;i++)await PrinterTransport.SendAsync(printer,bytes,cts.Token);await api.CompleteAsync(job.Id,cts.Token);Console.WriteLine($"{DateTime.Now:T} Job {job.Id} -> {printer.Name}: IMPRESSO");}
                catch(Exception ex){try{await api.FailAsync(job.Id,ex.Message,cts.Token);}catch{}Console.WriteLine($"{DateTime.Now:T} Job {job.Id}: ERRO - {ex.Message}");}
            }
        }
        catch(OperationCanceledException){break;}
        catch(Exception ex){Console.WriteLine($"{DateTime.Now:T} Falha de comunicacao: {ex.Message}");}
        await Task.Delay(TimeSpan.FromSeconds(3),cts.Token);
    }
}
catch(Exception ex){Console.Error.WriteLine($"Falha ao iniciar: {ex.Message}");Environment.ExitCode=1;}
static int ReadCopies(string json){try{using var d=System.Text.Json.JsonDocument.Parse(json);if(d.RootElement.TryGetProperty("copias",out var c)&&c.TryGetInt32(out var n))return Math.Clamp(n,1,10);}catch{}return 1;}
static string ReadPassword(){var chars=new List<char>();while(true){var k=Console.ReadKey(true);if(k.Key==ConsoleKey.Enter){Console.WriteLine();return new string(chars.ToArray());}if(k.Key==ConsoleKey.Backspace&&chars.Count>0){chars.RemoveAt(chars.Count-1);continue;}if(!char.IsControl(k.KeyChar)){chars.Add(k.KeyChar);Console.Write('*');}}}
