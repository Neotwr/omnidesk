# OmniDesk

[![Version](https://img.shields.io/badge/version-1.2.0-blue.svg)](https://github.com/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](https://dotnet.microsoft.com/)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)

Aplicação utilitária para Windows desenvolvida em **C# / .NET 10** e **WPF** com padrão de arquitetura **MVVM Moderno (`CommunityToolkit.Mvvm`)**, projetada para otimizar e acelerar as atividades operacionais de suporte e sustentação de TI.

---

## ✨ Principais Recursos

### 1. 🖥️ Acesso Remoto Inteligente
- Inicie sessões de **Assistência Remota do Windows** (`msra.exe /offerra`) ou **Área de Trabalho Remota** (`mstsc.exe`) informando apenas o **patrimônio** ou **endereço IP**.
- **Autenticação Automática de Serviço**: Integração sob demanda com conta de serviço/admin (DCOM nativo para MSRA e NLA para RDP com limpeza automática de credenciais temporárias do Windows).
- **Gerenciamento Rápido**: Botão de chave (**🔑**) no canto inferior direito para troca e atualização de credenciais a qualquer momento.
- Suporte a disparo instantâneo via tecla **Enter**.

### 2. 👥 Comparador de Acessos Multi-Origem (Active Directory, SAP e Senior)
- **Comparação Inteligente de Acessos**: Compara dois usuários (Alvo vs Referência) e exibe com precisão **apenas os acessos, grupos, perfis ou telas que o usuário de referência possui e que faltam no usuário alvo**.
- **Seleção Multi-Origem**:
  - **AD**: Compara grupos de segurança e distribuição do Active Directory utilizando a conta de serviço.
  - **SAP**: Compara perfis e roles atribuídos no SAP NetWeaver / ERP utilizando o ambiente atualmente ativo na aplicação.
  - **Senior**: Compara telas e perfis de RH / SGU no Senior (Vetorh / ServiceNow).
- **Filtro Instantâneo**: Janela de resultados com busca textual em tempo real sobre nome e descrição dos grupos.
- Suporte a disparo instantâneo via tecla **Enter**.

### 3. 🔑 Consulta de Acessos Multi-Ambiente (AD, Senior e SAP)
- **Active Directory**: Consulta todos os grupos de segurança e distribuição do usuário no domínio via conta de serviço.
- **Senior (Vetorh / SGU)**:
  - Integração nativa em segundo plano com o portal de atendimento corporativo (ServiceNow / WBS).
  - Autenticação silenciosa via **Single Sign-On (SSO)** com Microsoft Entra ID usando **WebView2** invisível rodando sob o usuário logado do Windows.
  - Consulta instantânea de perfis e telas de RH/SGU atribuídos ao colaborador.
- **SAP ERP / NetWeaver**:
  - Integração via **SAP .NET Connector 3.0 (NCo)** executando a BAPI `BAPI_USER_GET_DETAIL`.
  - **Descoberta Automática de Ambientes**: Carrega todas as conexões cadastradas no `SAPUILandscape.xml` / `saplogon.ini` da máquina do operador.
  - **Suporte Multi-Ambiente**: Permite alternar facilmente entre ambientes (Produção, Qualidade, Desenvolvimento, etc.) com sessões isoladas por servidor.
  - **Armazenamento Seguro (DPAPI)**: Opção de salvar credenciais com criptografia nativa do Windows (`ProtectedData`), garantindo que senhas nunca sejam salvas em texto plano.

### 4. 🛠️ Painel de Utilitários de Administração (Utils)
- **Lançador Rápido de Ferramentas Administrativas**: Acesso centralizado com um clique para ferramentas operacionais do Windows:
  - **Gerenciamento do Computador** (`compmgmt.msc`)
  - **Active Directory Users and Computers** (`dsa.msc`)
  - **Área de Trabalho Remota** (`mstsc.exe`)
- **Execução com Credenciais de Serviço**: Inicialização transparente de consoles de gerenciamento (.msc via MMC) e executáveis utilizando as credenciais da conta de serviço (`CreateProcessWithLogonW`), eliminando a necessidade de "Executar como outro usuário" manualmente.
- **Interface Otimizada**: Painel em grade com suporte a barras de rolagem finas e personalizadas (`ThinScrollViewerStyle`).

---

## 🏛️ Arquitetura e Tecnologias

- **Framework**: [.NET 10.0 (Windows)](https://dotnet.microsoft.com/download)
- **Interface**: WPF (Windows Presentation Foundation) com estilos e scrollbars customizadas
- **Padrão de Projeto**: MVVM Moderno com `CommunityToolkit.Mvvm` (Source Generators, `ObservableProperty`, `RelayCommand`)
- **Integração Active Directory**: `System.DirectoryServices.AccountManagement` com credenciais dinâmicas
- **Integração Senior / WBS**: `Microsoft.Web.WebView2` com SSO transparente e execução de requisições assíncronas isoladas
- **Integração SAP**: SAP .NET Connector 3.0 x64 via `SapLoadContext` compatível com CoreCLR .NET 10
- **Execução de Processos e Logon**: `ProcessLogonHelper` via Win32 `advapi32.dll` (`CreateProcessWithLogonW`)
- **Segurança e Cofre de Credenciais**:
  - `ServiceAuthManager`: Gerenciamento centralizado de conta de serviço para AD, Comparador, Acesso Remoto e Utilitários.
  - Criptografia de credenciais via Windows DPAPI (`System.Security.Cryptography.ProtectedData`).
  - Configurações corporativas embutidas em memória (`EmbeddedResource`) via `appsettings.json`, sem gerar arquivos extras na pasta do executável e com isolamento total no Git para evitar exposição de URLs internas.

---

## 🛠️ Pré-requisitos

1. **Sistema Operacional**: Windows 10/11 (64-bit).
2. **SDK**: [.NET 10.0 SDK](https://dotnet.microsoft.com/download) instalado.
3. **Ambiente de Rede**: Acesso ao domínio corporativo do Active Directory.
4. **WebView2 Runtime**: Geralmente pré-instalado nativamente no Windows 10/11 (Microsoft Edge WebView2 Runtime).
5. **Dependências SAP**: Para executar a consulta SAP, os binários do SAP NCo 3.0 64-bit devem estar presentes na raiz do projeto.

---

## ⚙️ Configuração Local

Ao clonar o repositório, copie o arquivo de exemplo de configurações e preencha com os endpoints do seu ambiente:

```powershell
Copy-Item appsettings.example.json appsettings.json
```

Estrutura do `appsettings.json`:
```json
{
  "WbsSettings": {
    "BaseUrl": "https://seu-portal.empresa.com",
    "SsoId": "SEU_GLIDE_SSO_ID_AQUI",
    "CatalogSysId": "SYS_ID_DO_CATALOGO_AQUI",
    "WidgetSysId": "SYS_ID_DO_WIDGET_AQUI"
  }
}
```
> *Nota: O arquivo `appsettings.json` está incluído no `.gitignore` e nunca será comitado para o repositório remoto.*

---

## 🚀 Compilar e Executar

1. Abra um terminal no diretório do projeto.
2. Para compilar em modo Release:
   ```powershell
   dotnet build -c Release
   ```
3. Para executar diretamente:
   ```powershell
   dotnet run
   ```

Alternativamente, abra `OmniDesk.slnx` no Visual Studio e execute a solução.

### Gerar Executável Único para Distribuição (Single-File):
```powershell
dotnet publish -c Release
```
O executável independente, autocontido e sem dependências externas será gerado na pasta:
`bin\Release\net10.0-windows\win-x64\publish\OmniDesk.exe`

---

## 📦 Dependências

- **`System.DirectoryServices.AccountManagement`**: Usado para autenticar e consultar grupos de usuários no Active Directory.
- **`Microsoft.Web.WebView2`**: Motor do Microsoft Edge integrado para execução de requisições autenticadas e SSO corporativo.
- **`CommunityToolkit.Mvvm`**: Framework oficial da Microsoft para implementação do padrão MVVM com alta performance e Source Generators.
- **`Microsoft.CodeAnalysis.CSharp` (Roslyn)**: Compilação dinâmica de stubs de compatibilidade para suporte a bibliotecas legadas no runtime do .NET 10.
- **`SAP .NET Connector 3.0 (NCo)`**: SDK oficial da SAP para comunicação RFC de alta performance e execução da BAPI `BAPI_USER_GET_DETAIL`.

---

## 🤝 Contribuição e Guia do Desenvolvedor

Deseja contribuir com o OmniDesk ou adicionar novas funções, utilitários ou integrações? Consulte o nosso guia completo de desenvolvimento:

👉 **[Guia de Arquitetura e Contribuição (CONTRIBUTING.md)](CONTRIBUTING.md)**

---

## 📄 Licença

Este projeto está licenciado sob a licença GNU General Public License v3.0 (GPL-3.0). Consulte o arquivo [LICENSE](LICENSE) para mais informações.
