# NAXAPA Print Agent

Agente local de impressão em .NET 10 para trabalhos persistidos pelo NAXAPA.

- Faz login na API sem guardar a senha.
- Busca fila pendente.
- Faz `claim` atômico do trabalho.
- Envia impressão por TCP/IP (ESC/POS) ou spooler RAW do Windows.
- Confirma `IMPRESSO` apenas depois do envio ao transporte local; falhas retornam `ERRO` para a fila.
- Respeita número de cópias do payload.

Observação: a confirmação disponível em ESC/POS/spooler normalmente comprova aceitação pelo transporte/spooler, não que o papel fisicamente saiu. Impressoras com protocolo de status bidirecional exigem integração específica do fabricante.

A compatibilidade física com cada modelo Epson/Elgin/Tanca não pôde ser testada no ambiente de geração por ausência do hardware.
