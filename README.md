# OmniDesk

Aplicação utilitária para Windows desenvolvida em **C# / .NET 10** e **WPF** com padrão de arquitetura **MVVM Moderno (`CommunityToolkit.Mvvm`)**, projetada para otimizar e acelerar as atividades operacionais de suporte e sustentação de TI.

---

## ✨ Principais Recursos

### 1. 🖥️ Acesso Remoto
- Inicie sessões de **Assistência Remota do Windows** (`msra.exe /offerra`) ou **Área de Trabalho Remota** (`mstsc.exe`) informando apenas o **patrimônio** ou **endereço IP**.
- Suporte a disparo instantâneo via tecla **Enter**.

### 2. 👥 Comparador de Grupos do Active Directory
- **Busca Rápida**: Consulte os grupos de qualquer usuário do domínio individualmente através do botão de lupa 🔍.
- **Comparação Inteligente**: Compara dois usuários (Alvo vs Referência) e exibe com precisão **apenas os grupos que o usuário de referência possui e que faltam no usuário alvo**.
- **Filtro Instantâneo**: Janela de resultados com busca textual em tempo real sobre nome e descrição dos grupos.

### 3. 🔑 Consulta de Acessos Multi-Ambiente (AD & SAP)
- **Active Directory**: Consulta todos os grupos de segurança e distribuição do usuário no domínio.
- **SAP ERP / NetWeaver**:
  - Integração via **SAP .NET Connector 3.0 (NCo)** executando a BAPI `BAPI_USER_GET_DETAIL`.
  - **Descoberta Automática de Ambientes**: Carrega todas as conexões cadastradas no `SAPUILandscape.xml` / `saplogon.ini` da máquina do operador.
  - **Suporte Multi-Ambiente**: Permite alternar facilmente entre ambientes (Produção, Qualidade, Desenvolvimento, etc.) com sessões isoladas por servidor.
  - **Armazenamento Seguro (DPAPI)**: Opção de salvar credenciais com criptografia nativa do Windows (`ProtectedData`), garantindo que senhas nunca sejam salvas em texto plano.

---

## 🏛️ Arquitetura e Tecnologias

- **Framework**: [.NET 10.0 (Windows)](https://dotnet.microsoft.com/download)
- **Interface**: WPF (Windows Presentation Foundation)
- **Padrão de Projeto**: MVVM Moderno com `CommunityToolkit.Mvvm` (Source Generators, `ObservableProperty`, `RelayCommand`)
- **Integração Active Directory**: `System.DirectoryServices.AccountManagement`
- **Integração SAP**: SAP .NET Connector 3.0 x64 via `SapLoadContext` compatível com CoreCLR .NET 10
- **Segurança**: Criptografia de credenciais via Windows DPAPI (`System.Security.Cryptography.ProtectedData`)

---

## 🛠️ Pré-requisitos

1. **Sistema Operacional**: Windows 10/11 (64-bit).
2. **SDK**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download) instalado.
3. **Ambiente de Rede**: Acesso ao domínio do Active Directory da sua rede corporativa.
4. **Dependências SAP**: Para executar a consulta SAP, os binários do SAP NCo 3.0 64-bit devem estar presentes na raiz do projeto.

---

## 🚀 Compilar e Executar

1. Abra um terminal no diretório do projeto.
2. Para compilar:
   ```powershell
   dotnet build -c Release
   ```
3. Para executar diretamente:
   ```powershell
   dotnet run
   ```

Alternativamente, abra `OmniDesk.slnx` no Visual Studio e execute a solução.

### Gerar Executável Único para Distribuição:
```powershell
dotnet publish -c Release
```
O executável independente e autocontido será gerado na pasta:
`bin\Release\net10.0-windows\win-x64\publish\OmniDesk.exe`

---

## 📦 Dependências

- **`System.DirectoryServices.AccountManagement`**: Usado para autenticar e consultar grupos de usuários no Active Directory.
- **`CommunityToolkit.Mvvm`**: Framework oficial da Microsoft para implementação do padrão MVVM com alta performance e Source Generators.
- **`Microsoft.CodeAnalysis.CSharp` (Roslyn)**: Compilação dinâmica de stubs de compatibilidade para suporte a bibliotecas legadas no runtime do .NET 10.
- **`SAP .NET Connector 3.0 (NCo)`**: SDK oficial da SAP para comunicação RFC de alta performance e execução da BAPI `BAPI_USER_GET_DETAIL`.

---

## 📄 Licença

Este projeto está licenciado sob a licença GNU General Public License v3.0 (GPL-3.0). Consulte o arquivo [LICENSE](LICENSE) para mais informações.
