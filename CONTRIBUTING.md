# 📘 Guia de Arquitetura e Contribuição — OmniDesk

Seja bem-vindo ao guia de desenvolvimento do **OmniDesk**! 

Este documento foi criado para ajudar novos desenvolvedores e colaboradores a entenderem a arquitetura do projeto, as tecnologias utilizadas, as boas práticas adotadas e o **passo a passo detalhado para implementar novas funcionalidades** de forma rápida, consistente e padronizada.

---

## 📑 Índice

1. [Visão Geral e Tecnologias](#-visão-geral-e-tecnologias)
2. [Estrutura do Projeto e Responsabilidades](#-estrutura-do-projeto-e-responsabilidades)
3. [Padrão Arquitetural (MVVM Moderno)](#-padrão-arquitetural-mvvm-moderno)
4. [Como Adicionar Novas Funcionalidades](#-como-adicionar-novas-funcionalidades)
   - [Cenário 1: Adicionar um novo Utilitário na aba 'Utils'](#cenário-1-adicionar-um-novo-utilitário-na-aba-utils)
   - [Cenário 2: Adicionar uma nova Origem de Dados / Integração Externa](#cenário-2-adicionar-uma-nova-origem-de-dados--integração-externa)
   - [Cenário 3: Criar uma Nova Janela ou Diálogo Modal](#cenário-3-criar-uma-nova-janela-ou-diálogo-modal)
5. [Padrões de Código e Boas Práticas](#-padrões-de-código-e-boas-práticas)
   - [Uso de CommunityToolkit.Mvvm](#uso-de-communitytoolkitmvvm)
   - [Desacoplamento e Injeção de Dependências](#desacoplamento-e-injeção-de-dependências)
   - [Tratamento de Threads e Assincronismo](#tratamento-de-threads-e-assincronismo)
   - [Exibições de Feedback e Modais (IDialogService)](#exibições-de-feedback-e-modais-idialogservice)
   - [Segurança e Persistência de Credenciais (DPAPI)](#segurança-e-persistência-de-credenciais-dpapi)
6. [Particularidades Técnicas do OmniDesk](#-particularidades-técnicas-do-omnidesk)
   - [SAP .NET Connector 3.0 em .NET 10 (SapLoadContext)](#sap-net-connector-30-em-net-10-saploadcontext)
   - [WebView2 com SSO Silencioso (Senior/WBS)](#webview2-com-sso-silencioso-seniorwbs)
   - [Configurações Embutidas (appsettings.json)](#configurações-embutidas-appsettingsjson)
7. [Ambiente, Compilação e Execução](#-ambiente-compilação-e-execução)
8. [Fluxo de Pull Request e Commits](#-fluxo-de-pull-request-e-commits)

---

## 🚀 Visão Geral e Tecnologias

O **OmniDesk** é uma aplicação desktop para Windows desenvolvida com as seguintes tecnologias principais:

- **Plataforma / Runtime**: [.NET 10.0 (Windows x64)](https://dotnet.microsoft.com/download)
- **Interface Gráfica (UI)**: **WPF** (Windows Presentation Foundation) com XAML
- **Toolkit MVVM**: `CommunityToolkit.Mvvm` (com Source Generators C#)
- **Integração Active Directory**: `System.DirectoryServices.AccountManagement`
- **Navegador Embutido & SSO**: `Microsoft.Web.WebView2`
- **Integração SAP**: SAP .NET Connector (NCo 3.0) via `SapLoadContext` isolado
- **Compilação Dinâmica em Runtime**: `Microsoft.CodeAnalysis.CSharp` (Roslyn)
- **Criptografia Nativa**: Windows DPAPI (`System.Security.Cryptography.ProtectedData`)

---

## 📂 Estrutura do Projeto e Responsabilidades

O código-fonte segue uma separação rigorosa de responsabilidades:

```text
OmniDesk/
├── Models/                        # DTOs, entidades de domínio e estruturas de dados
│   ├── ComparacaoGruposResultado.cs
│   ├── Grupos.cs
│   ├── SapUserSession.cs
│   ├── ServiceAccountCredentials.cs
│   └── WbsSettings.cs
│
├── Services/                      # Regras de negócio e integrações externas
│   ├── Abstractions/              # Interfaces / Contratos (Desacoplamento e Mocks)
│   │   ├── IActiveDirectoryService.cs
│   │   ├── IAppConfigService.cs
│   │   ├── IDialogService.cs
│   │   ├── IGrupoComparerService.cs
│   │   ├── IRemoteAccessService.cs
│   │   ├── ISapAuthManager.cs
│   │   ├── ISapService.cs
│   │   ├── ISeniorService.cs
│   │   └── IServiceAuthManager.cs
│   └── Implementations/           # Classes concretas de serviços
│       ├── ActiveDirectoryService.cs
│       ├── AppConfigService.cs
│       ├── DialogService.cs
│       ├── GrupoComparerService.cs
│       ├── RemoteAccessService.cs
│       ├── SapAuthManager.cs
│       ├── SapLoadContext.cs
│       ├── SapService.cs
│       ├── SeniorService.cs
│       └── ServiceAuthManager.cs
│
├── ViewModels/                    # Lógica de apresentação, comandos e estado da UI
│   ├── AcessoRemotoViewModel.cs
│   ├── ComparadorGruposViewModel.cs
│   ├── ConsultaAcessosViewModel.cs
│   ├── GruposViewModel.cs
│   ├── MainViewModel.cs
│   └── SapLoginViewModel.cs
│
├── Views/                         # Telas, Janelas e Diálogos em XAML
│   ├── GruposWindow.xaml (.cs)
│   ├── MainWindow.xaml (.cs)
│   ├── SapLoginDialog.xaml (.cs)
│   └── ServiceLoginDialog.xaml (.cs)
│
├── App.xaml (.cs)                 # Configuração de estilos globais e inicialização
├── appsettings.example.json       # Exemplo de configurações para ambiente local
└── OmniDesk.csproj                # Definição do projeto, pacotes NuGet e targets de build
```

---

## 🏛️ Padrão Arquitetural (MVVM Moderno)

O projeto adota o padrão **Model-View-ViewModel (MVVM)** potencializado pelo pacote oficial `CommunityToolkit.Mvvm`:

```
┌─────────────────┐       Data Binding & Comandos       ┌────────────────────────┐
│  Views (XAML)   │ <=================================> │  ViewModels (C#)       │
└─────────────────┘                                     └───────────┬────────────┘
                                                                    │ Chama Métodos
                                                                    ▼
                                                        ┌────────────────────────┐
                                                        │ Services (Interfaces)  │
                                                        └───────────┬────────────┘
                                                                    │ Implementado por
                                                                    ▼
                                                        ┌────────────────────────┐
                                                        │ Services (Concretos)   │
                                                        └───────────┬────────────┘
                                                                    │ Manipula / Retorna
                                                                    ▼
                                                        ┌────────────────────────┐
                                                        │ Models (POCOs / DTOs)  │
                                                        └────────────────────────┘
```

- **Views**: Apenas definem layout e controles visuais em XAML. Não contêm regras de negócio no code-behind (`.xaml.cs`).
- **ViewModels**: Herdam de `ObservableObject`. Mantêm o estado da tela, recebem ações do usuário via comandos (`[RelayCommand]`) e notificam a tela sobre mudanças de propriedades (`[ObservableProperty]`).
- **Services (Abstractions & Implementations)**: Executam operações pesadas, consultas a redes, processos externos, APIs e cálculos. Comunicações com a UI ocorrem indiretamente através de `IDialogService`.
- **Models**: Estruturas simples (POCOs/DTOs) que representam dados trafegados entre serviços e telas.

---

## 🛠️ Como Adicionar Novas Funcionalidades

Abaixo estão os 3 tutoriais práticos mais comuns para estender o OmniDesk:

---

### Cenário 1: Adicionar um novo Utilitário na aba 'Utils'

Suponha que você queira adicionar um novo botão para abrir o **Gerenciador de Dispositivos** (`devmgmt.msc`) ou executar um diagnóstico de rede.

#### Passo 1: Adicionar a operação no serviço apropriado (ou criar um novo serviço)

Se envolver processos do sistema ou acesso remoto, adicione o método na interface `IRemoteAccessService.cs`:

```csharp
// Services/Abstractions/IRemoteAccessService.cs
public interface IRemoteAccessService
{
    void IniciarAssistencialRemota(string destino, bool type, ServiceAccountCredentials? credenciais = null);
    void AbrirGerenciadorDispositivos(string? computador = null); // <-- Novo método
}
```

Implemente em `Services/Implementations/RemoteAccessService.cs`:

```csharp
// Services/Implementations/RemoteAccessService.cs
public void AbrirGerenciadorDispositivos(string? computador = null)
{
    var args = string.IsNullOrWhiteSpace(computador) ? "" : $"/computer={computador.Trim()}";
    ExecutarProcessoDireto("devmgmt.msc", args);
}
```

#### Passo 2: Criar o comando no ViewModel

No ViewModel correspondente (por exemplo, `AcessoRemotoViewModel.cs` ou um novo `UtilsViewModel.cs`):

```csharp
// ViewModels/AcessoRemotoViewModel.cs
[RelayCommand]
public void AbrirGerenciadorDispositivos()
{
    try
    {
        _remoteAccessService.AbrirGerenciadorDispositivos(Destino);
    }
    catch (Exception ex)
    {
        _dialogService.ShowError(ex.Message, "Erro ao abrir Utilitário");
    }
}
```

#### Passo 3: Vincular o comando no botão XAML (`Views/MainWindow.xaml`)

```xml
<!-- Views/MainWindow.xaml na aba Utils -->
<Button x:Name="btnDevMgmt" 
        Content="Gerenciador de Dispositivos"
        DataContext="{Binding AcessoRemoto}"
        Command="{Binding AbrirGerenciadorDispositivosCommand}" />
```

---

### Cenário 2: Adicionar uma nova Origem de Dados / Integração Externa

Suponha que você queira adicionar uma nova origem de consulta e comparação de permissões (ex: **Microsoft Entra ID / Azure AD** ou **Oracle ERP**).

#### Passo 1: Criar o Contrato da Abstração

Crie o arquivo `Services/Abstractions/IAzureAdService.cs`:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using OmniDesk.Models;

namespace OmniDesk.Services.Abstractions
{
    public interface IAzureAdService
    {
        Task<List<Grupos>> ObterGruposDoUsuarioAsync(string userPrincipalName);
    }
}
```

#### Passo 2: Implementar o Serviço Concreto

Crie `Services/Implementations/AzureAdService.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OmniDesk.Models;
using OmniDesk.Services.Abstractions;

namespace OmniDesk.Services.Implementations
{
    public class AzureAdService : IAzureAdService
    {
        public async Task<List<Grupos>> ObterGruposDoUsuarioAsync(string userPrincipalName)
        {
            if (string.IsNullOrWhiteSpace(userPrincipalName))
                throw new ArgumentException("O usuário não pode ser vazio.", nameof(userPrincipalName));

            return await Task.Run(() =>
            {
                // Realiza a chamada REST/Graph API ou SDK
                var lista = new List<Grupos>();
                
                // Exemplo de preenchimento:
                lista.Add(new Grupos
                {
                    Nome = "AZ-TI-Administradores",
                    Descricao = "Grupo de administradores no Azure",
                    Origem = "Azure AD"
                });

                return lista.OrderBy(g => g.Nome).ToList();
            });
        }
    }
}
```

#### Passo 3: Injetar o Novo Serviço no `MainViewModel` e `ConsultaAcessosViewModel`

1. Em `ViewModels/ConsultaAcessosViewModel.cs`:
   - Adicione a interface no construtor.
   - Crie uma propriedade `[ObservableProperty] private bool _isAzureChecked;` para o RadioButton.
   - Trate a chamada no método `ConsultarAsync()`.

```csharp
// Em ConsultaAcessosViewModel.cs:
[ObservableProperty]
private bool _isAzureChecked;

// No ConsultarAsync():
else if (IsAzureChecked)
{
    var grupos = await _azureAdService.ObterGruposDoUsuarioAsync(usuarioAlvo);
    _dialogService.ShowGruposWindow(grupos, $"Grupos Azure AD de: {usuarioAlvo}", "Origem: Azure Entra ID");
}
```

2. Em `ViewModels/MainViewModel.cs`:
   - Instancie o `AzureAdService` no construtor padrão e repasse para os ViewModels filhos.

#### Passo 4: Adicionar a Opção na Tela (`Views/MainWindow.xaml`)

Adicione o `RadioButton` na interface gráfica:

```xml
<RadioButton
    x:Name="rbAcessoAzure"
    Margin="10,0,0,0"
    Content="Azure AD"
    GroupName="TipoAcesso"
    IsChecked="{Binding IsAzureChecked}" />
```

---

### Cenário 3: Criar uma Nova Janela ou Diálogo Modal

Quando precisar de uma nova janela (ex: diálogo de configurações, relatório customizado):

1. **Crie a View**: Adicione `MinhaJanela.xaml` e `MinhaJanela.xaml.cs` na pasta `Views/`.
2. **Crie o ViewModel**: Adicione `MinhaJanelaViewModel.cs` na pasta `ViewModels/` com as propriedades observáveis e comandos.
3. **Desacople a Abertura via `IDialogService`**:
   - Adicione o método de exibição na interface `Services/Abstractions/IDialogService.cs`.
   - Implemente a instanciação da janela dentro de `Services/Implementations/DialogService.cs` utilizando `Application.Current.Dispatcher.Invoke(...)`.
4. **Dispare a partir do ViewModel**: Chame `_dialogService.ShowMinhaJanela(...)` sem instanciar controles WPF diretamente dentro dos ViewModels.

---

## 🎯 Padrões de Código e Boas Práticas

### Uso de CommunityToolkit.Mvvm

O projeto utiliza os **Source Generators** do C#. Sempre prefira os atributos da biblioteca:

- **Propriedades Observáveis**:
  ```csharp
  [ObservableProperty]
  private string _usuario = string.Empty;
  // O compilador gera automaticamente a propriedade pública:
  // public string Usuario { get => _usuario; set => SetProperty(...); }
  ```
- **Reação a Mudanças de Propriedades**:
  ```csharp
  partial void OnUsuarioChanged(string value)
  {
      // Código executado automaticamente quando Usuario mudar
  }
  ```
- **Comandos**:
  ```csharp
  [RelayCommand]
  public async Task ExecutarOperacaoAsync()
  {
      // O gerador cria: public IAsyncRelayCommand ExecutarOperacaoCommand { get; }
  }
  ```

---

### Desacoplamento e Injeção de Dependências

- **Sempre crie interfaces**: Toda nova integração externa deve ter sua interface em `Services/Abstractions/`.
- **Construtores Flexíveis**: Forneça um construtor com injeção de dependências (útil para testes unitários com mocks) e um construtor sem parâmetros ou padrão com fallbacks para inicialização do runtime.

```csharp
public class MeuViewModel : ObservableObject
{
    private readonly IMeuService _meuService;

    // Construtor padrão (Runtime/WPF)
    public MeuViewModel() : this(new MeuService()) { }

    // Construtor para Injeção de Dependência / Testes Unitários
    public MeuViewModel(IMeuService meuService)
    {
        _meuService = meuService ?? throw new ArgumentNullException(nameof(meuService));
    }
}
```

---

### Tratamento de Threads e Assincronismo

- **Nunca bloqueie a Thread de UI**: Utilize `Task.Run` dentro dos serviços para operações síncronas que acessam rede, DCOM, BAPIs ou chamadas Win32.
- **Tratamento de Concorrência**: Use `SemaphoreSlim(1, 1)` para recursos que não podem ser inicializados simultaneamente (como feito em `SeniorService.cs`).
- **Estado de Carregamento**: Utilize propriedades como `[ObservableProperty] private bool _isBusy;` para desabilitar botões ou exibir spinners enquanto a tarefa executa.

---

### Exibições de Feedback e Modais (IDialogService)

Nunca utilize `MessageBox.Show()` ou `new MinhaJanela().Show()` diretamente dentro de **ViewModels** ou **Services**.

Utilize sempre o `IDialogService`:
```csharp
_dialogService.ShowWarning("Por favor, preencha todos os campos.", "Aviso");
_dialogService.ShowError(ex.Message, "Erro na Operação");
_dialogService.ShowInfo("Processo finalizado com sucesso!", "Sucesso");
_dialogService.ShowGruposWindow(listaGrupos, "Título da Consulta", "Subtítulo explicativo");
```

---

### Segurança e Persistência de Credenciais (DPAPI)

Ao armazenar credenciais ou dados sensíveis localmente:
- **Nunca salve senhas em texto plano**.
- Utilize **Windows DPAPI** (`ProtectedData.Protect` com `DataProtectionScope.CurrentUser`), que criptografa os bytes utilizando a chave de segurança vinculada à conta do usuário logado no Windows.
- Veja `ServiceAuthManager.cs` e `SapAuthManager.cs` como referências de implementação.

---

## 🧩 Particularidades Técnicas do OmniDesk

### SAP .NET Connector 3.0 em .NET 10 (SapLoadContext)

As DLLs do SAP NCo (`sapnco.dll` e `sapnco_utils.dll`) foram originalmente projetadas para o .NET Framework e exigem assemblies legados (como `System.Web`, `System.ServiceModel` e `System.Management.Instrumentation`).

Para que funcionem no **.NET 10 CoreCLR**:
1. O OmniDesk utiliza um `AssemblyLoadContext` isolado (`Services/Implementations/SapLoadContext.cs`).
2. Quando o conector SAP requisita referências ausentes, o **Roslyn** compila em memória stubs leves com a mesma assinatura pública.
3. No `OmniDesk.csproj`, as targets `FilterSapFromMarkupCompilePass1/2` removem as DLLs do SAP durante a compilação do XAML para evitar conflitos com atributos WMI.

> **Importante**: Ao trabalhar com o SAP, faça chamadas através de `ISapService`, preservando o isolamento por Reflection e LoadContext.

---

### WebView2 com SSO Silencioso (Senior/WBS)

A integração com o portal corporativo Senior/WBS (ServiceNow) utiliza o `Microsoft.Web.WebView2` em segundo plano:
- Executa em modo invisível (dimensões `0x0` colapsado na árvore visual).
- Autentica via **Single Sign-On (SSO) do Microsoft Entra ID** aproveitando a conta corporativa ativa do Windows (`AllowSingleSignOnUsingOSPrimaryAccount = true`).
- A comunicação com os scripts JavaScript das páginas é feita via `window.chrome.webview.postMessage` e deserialização de JSON.

---

### Configurações Embutidas (appsettings.json)

- O arquivo `appsettings.json` é configurado como `EmbeddedResource` no projeto (`OmniDesk.csproj`).
- Isso permite que o executável publicado em formato *Single-File* contenha os endpoints corporativos sem expor URLs em arquivos externos na pasta de publicação.
- Para testes locais, copie `appsettings.example.json` para `appsettings.json` na raiz. O arquivo real é ignorado pelo Git (`.gitignore`).

---

## 💻 Ambiente, Compilação e Execução

### Pré-requisitos

1. **Windows 10 ou 11 (64-bit)**
2. **[.NET 10.0 SDK](https://dotnet.microsoft.com/download)** ou superior
3. **Visual Studio 2022+** (com carga de trabalho *Desenvolvimento para desktop com .NET*) ou **VS Code** (com extensão C# Dev Kit)
4. **WebView2 Runtime** (já incluído no Windows 10/11)

### Comandos Úteis no Terminal (PowerShell)

```powershell
# 1. Restaurar dependências
dotnet restore

# 2. Compilar em modo Debug
dotnet build

# 3. Executar o projeto
dotnet run

# 4. Compilar e gerar executável único (Single-File) autocontido
dotnet publish -c Release
```

O executável único para distribuição será gerado em:
`bin\Release\net10.0-windows\win-x64\publish\OmniDesk.exe`

---

## 🤝 Fluxo de Pull Request e Commits

1. **Crie uma branch descritiva** para sua tarefa a partir da `main`:
   ```powershell
   git checkout -b feature/novo-utilitario-ad
   # ou
   git checkout -b fix/ajuste-validacao-sap
   ```
2. **Mantenha os commits objetivos**:
   - `feat: adiciona atalho para gerenciador de discos na aba utils`
   - `fix: corrige tratamento de timeout na consulta senior`
   - `docs: atualiza guia de contribuicao`
3. **Verifique a compilação local**: Certifique-se de que `dotnet build` e `dotnet publish` passam sem warnings ou erros antes de submeter o PR.
4. **Abra o Pull Request** descrevendo detalhadamente o que foi alterado e como testar.

---

*Obrigado por colaborar com o OmniDesk! Dúvidas ou sugestões? Abra uma Issue ou entre em contato com os mantenedores do projeto.*
