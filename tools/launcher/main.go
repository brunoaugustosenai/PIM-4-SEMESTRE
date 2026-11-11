package main
import(
 "archive/zip";"bufio";"embed";"fmt";"io";"net";"os";"os/exec";"path/filepath";"runtime";"strings";"time"
)
//go:embed site.zip
var embedded embed.FS
func main(){
 if runtime.GOOS!="windows"{fmt.Println("Este inicializador foi criado para Windows.");return}
 fmt.Println("NAXAPA Gestão / API - .NET 10");fmt.Println()
 dotnet:=findDotnet();if dotnet==""{fatal(".NET 10 SDK não encontrado. Execute primeiro o instalador de dependências.")}
 ver,err:=exec.Command(dotnet,"--version").CombinedOutput();if err!=nil||!strings.HasPrefix(strings.TrimSpace(string(ver)),"10."){fatal("É necessário o .NET 10 SDK. Versão encontrada: "+strings.TrimSpace(string(ver)))}
 local,_:=os.UserCacheDir();target:=filepath.Join(local,"NAXAPA","GestaoV3");if err:=extract(target);err!=nil{fatal("Falha ao preparar os arquivos: "+err.Error())}
 project:=filepath.Join(target,"Naxapa.Web","Naxapa.Web.csproj");logPath:=filepath.Join(target,"naxapa-startup.log")
 fmt.Println("Preparando pacotes NuGet...");if out,err:=exec.Command(dotnet,"restore",project,"--nologo").CombinedOutput();err!=nil{os.WriteFile(logPath,out,0644);fatal("dotnet restore falhou. Consulte: "+logPath+"\n"+tail(string(out),1800))}
 fmt.Println("Validando compilação...");if out,err:=exec.Command(dotnet,"build",project,"-c","Release","--no-restore","--nologo").CombinedOutput();err!=nil{os.WriteFile(logPath,out,0644);fatal("A compilação falhou. Consulte: "+logPath+"\n"+tail(string(out),2200))}
 url:="http://localhost:5180";cmd:=exec.Command(dotnet,"run","--project",project,"-c","Release","--no-build","--urls","http://0.0.0.0:5180");cmd.Dir=filepath.Dir(project);lf,_:=os.Create(logPath);defer lf.Close();cmd.Stdout=io.MultiWriter(os.Stdout,lf);cmd.Stderr=io.MultiWriter(os.Stderr,lf)
 if err:=cmd.Start();err!=nil{fatal("Não foi possível iniciar o ASP.NET Core: "+err.Error())}
 go func(){cmd.Wait()}();fmt.Println("\nAguardando o servidor...");if !waitPort("127.0.0.1:5180",45*time.Second){_ = cmd.Process.Kill();fatal("O servidor não respondeu na porta 5180. Consulte: "+logPath)}
 fmt.Println("\nNAXAPA disponível:");fmt.Println("  Neste computador: "+url);for _,ip:=range lanIPs(){fmt.Printf("  Na mesma rede:     http://%s:5180\n",ip)}
 openBrowser(url);fmt.Println("\nMantenha esta janela aberta. Pressione ENTER para encerrar o NAXAPA.");bufio.NewReader(os.Stdin).ReadString('\n');_ = cmd.Process.Kill()
}
func findDotnet()string{if p,e:=exec.LookPath("dotnet.exe");e==nil{return p};p:=filepath.Join(os.Getenv("ProgramFiles"),"dotnet","dotnet.exe");if _,e:=os.Stat(p);e==nil{return p};return ""}
func extract(target string)error{b,e:=embedded.ReadFile("site.zip");if e!=nil{return e};tmp:=filepath.Join(os.TempDir(),"naxapa-site.zip");if e=os.WriteFile(tmp,b,0644);e!=nil{return e};defer os.Remove(tmp);r,e:=zip.OpenReader(tmp);if e!=nil{return e};defer r.Close();os.MkdirAll(target,0755);for _,f:=range r.File{clean:=filepath.Clean(f.Name);if strings.Contains(clean,"..")||filepath.IsAbs(clean){return fmt.Errorf("caminho inválido no pacote")};dst:=filepath.Join(target,clean);if f.FileInfo().IsDir(){os.MkdirAll(dst,0755);continue};os.MkdirAll(filepath.Dir(dst),0755);src,e:=f.Open();if e!=nil{return e};out,e:=os.Create(dst);if e!=nil{src.Close();return e};_,e=io.Copy(out,src);out.Close();src.Close();if e!=nil{return e}};return nil}
func waitPort(addr string,d time.Duration)bool{end:=time.Now().Add(d);for time.Now().Before(end){c,e:=net.DialTimeout("tcp",addr,700*time.Millisecond);if e==nil{c.Close();return true};time.Sleep(500*time.Millisecond)};return false}
func lanIPs()[]string{var out []string;ifs,_:=net.Interfaces();for _,i:=range ifs{if i.Flags&net.FlagUp==0||i.Flags&net.FlagLoopback!=0{continue};as,_:=i.Addrs();for _,a:=range as{var ip net.IP;switch v:=a.(type){case *net.IPNet:ip=v.IP;case *net.IPAddr:ip=v.IP};if ip==nil||ip.To4()==nil{continue};s:=ip.String();if strings.HasPrefix(s,"169.254."){continue};out=append(out,s)}};return out}
func openBrowser(url string){chrome:=[]string{filepath.Join(os.Getenv("ProgramFiles"),"Google","Chrome","Application","chrome.exe"),filepath.Join(os.Getenv("ProgramFiles(x86)"),"Google","Chrome","Application","chrome.exe"),filepath.Join(os.Getenv("LOCALAPPDATA"),"Google","Chrome","Application","chrome.exe")};for _,p:=range chrome{if _,e:=os.Stat(p);e==nil{exec.Command(p,url).Start();return}};exec.Command("rundll32","url.dll,FileProtocolHandler",url).Start()}
func tail(s string,n int)string{if len(s)<=n{return s};return s[len(s)-n:]}
func fatal(s string){fmt.Println("\nERRO: "+s);fmt.Println("\nPressione ENTER para fechar.");bufio.NewReader(os.Stdin).ReadString('\n');os.Exit(1)}
