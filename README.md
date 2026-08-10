# RemoteAccessUtil

Aplicação WPF em C# para auxiliar operadores de TI com duas funcionalidades principais:

- **Acesso remoto**: inicia uma sessão de Assistência Remota do Windows (`msra.exe /offerra`) para se conectar a outro computador usando patrimônio ou endereço IP.
- **Comparação de grupos do Active Directory**: pesquisa grupos de segurança de usuários do AD e exibe diferenças entre o usuário de referência e o usuário alvo.

## ✨ Recursos

- Interface WPF leve com abas para acesso remoto e comparação de grupos.
- Pesquisa de grupos do Active Directory por nome de usuário.
- Exibição e comparação de grupos que um usuário de referência possui e o usuário alvo não possui.
- Fácil de usar para tarefas administrativas em ambientes Windows/AD.

## 🛠️ Pré-requisitos

- Windows 10/11 ou superior.
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download).
- Acesso a um domínio Active Directory válido para usar a funcionalidade de grupos.
- `msra.exe` disponível no sistema para o recurso de Assistência Remota.

## 🚀 Compilar e Executar

1. Abra um terminal no diretório do projeto.
2. Execute:
   ```powershell
   dotnet build -c Release
   ```
3. Para executar diretamente:
   ```powershell
   dotnet run
   ```

Alternativamente, abra `RemoteAccessUtil.slnx` no Visual Studio e execute a solução.

## 📦 Dependências

- `System.DirectoryServices.AccountManagement` - usado para consultar grupos de usuários no Active Directory.

## 📄 Licença

Este projeto está licenciado sob a licença GNU General Public License v3.0 (GPL-3.0). Consulte o arquivo [LICENSE](LICENSE) para obter detalhes.
