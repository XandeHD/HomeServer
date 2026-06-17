# 🏠 Home Server

![Status](https://img.shields.io/badge/Status-Em%20Desenvolvimento-orange?style=for-the-badge)
![Tech](https://img.shields.io/badge/Stack-.NET%208%20%7C%20Blazor%20Server-purple?style=for-the-badge)

O **Home Server** é uma plataforma modular concebida exclusivamente para a gestão e centralização de informação pessoal (*Personal Information Management*). O sistema foi desenhado para ser expansível, permitindo a adição progressiva de novos módulos utilitários de forma independente.

---

## 🚀 Funcionalidades Atuais

Atualmente, o projeto foca-se na estrutura base de utilizadores, personalização e controlo financeiro/logístico básico:

### 👤 Gestão de Conta & Perfil
* **Autenticação Segura:** Integração com `ASP.NET Core Identity` para controlo de acessos e hashing de credenciais.
* **Customização Dinâmica:** Suporte a múltiplos temas visuais (Default/Azul, Vermelho, Verde, Amarelo) aplicados em tempo real por utilizador.

### 👥 Gestão de Grupos
* **Partilha Isolada:** Possibilidade de criar ou juntar-se a grupos através de códigos de convite dinâmicos de 6 caracteres.
* **Controlo Cirúrgico de Membros:** O proprietário do grupo pode remover utilizadores. 
* **Isolamento de Dados:** Caso um utilizador saia ou seja removido de um grupo, o sistema limpa os vínculos de grupo (`GroupId = 0`) de todos os seus registos históricos de forma automática e direta na base de dados, garantindo que a informação pessoal não fica órfã nem exposta.

### 📊 Módulos de Informação Pessoal (Finanças & Logística)
* **Salários (`Salary`):** Registo e acompanhamento de rendimentos.
* **Despesas (`Expense` & `ExpenseLines`):** Controlo detalhado de gastos com suporte a desmembramento por linhas.
* **Ordens de Compra (`BuyOrder` & `BuyOrderLines`):** Monitorização de compras e encomendas pendentes ou concluídas.

---

## 🛠️ Stack Tecnológica

* **Frontend & Backend:** [Blazor Server](https://learn.microsoft.com/en-us/aspnet/core/blazor/) (Modo `InteractiveServer`)
* **ORM / Acesso a Dados:** [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/) utilizando a abordagem de `IDbContextFactory` para otimização de concorrência em Blazor.
*
