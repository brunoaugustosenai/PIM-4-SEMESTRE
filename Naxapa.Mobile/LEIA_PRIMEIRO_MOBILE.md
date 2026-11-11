# NAXAPA Mobile — correção v3.5.0

O módulo anterior tinha duas causas principais de erro:

1. O código Dart calculava valores `double` usando `fold(0, ...)`, fazendo o analyzer inferir `int` e gerar incompatibilidade de tipos.
2. O pacote não trazia um scaffold Android pronto; ele dependia de um script auxiliar e isso deixava o módulo incompleto até a preparação.

Nesta versão, o código Dart foi corrigido e o scaffold Android é gerado automaticamente com **a mesma versão do Flutter instalada no computador**, evitando incompatibilidade de Gradle/Kotlin causada por templates antigos.

## Ordem correta

Na raiz do NAXAPA:

```powershell
.\PREPARAR_MOBILE.bat
.\VALIDAR_MOBILE.bat
```

Para executar:

```powershell
.\RUN_MOBILE.bat
```

Para um celular físico, abra outro terminal e rode:

```powershell
.\RUN_WEB_PARA_MOBILE.bat
```

Depois, na tela de login do app, informe a API como `http://IP-DO-PC:5180`.

No emulador Android, use `http://10.0.2.2:5180`.
