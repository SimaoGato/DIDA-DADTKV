# Checkpoint Report

## Como correr o projeto
1. Abrir visual studio
2. Escolher solução
3. Em "Propriedades de Configuração de SystemConfiguration" escrever em "Argumentos da linha de comando" o nome do script de configuração a ser corrido
4. Pôr os scripts do cliente dentro do Projeto SystemConfiguration
5. Lançar o Program do SystemConfiguration

## Feedback do projeto
- DadInt Implementados
- Ficheiro de Configuração é totalmente lido
    - Processos todos abrem corretamente (com o ficheiro de configuração de exemplo que foi dado)
    - Épocas já implementadas, com rondas de Paxos entre elas
- Clientes já fazem TxSubmit e Status aos Transaction Managers. No TxSubmit recebem imediatamente resposta do TM (ainda não se implementou a transação ao nível dos Transaction Manager)
- Transaction Managers (ainda não há transações, crashes, suspected, propagação e a lógica de ter um lease). Apenas ainda efetuam pedidos de lease e recebem o resultado das rondas de Paxos
- Lease Managers: praticamente todo implementado. Faltam crashes e suspected
- A nivel global, ainda não há uma boa sincronização (locks) e ainda não se trata de todos os erros possíveis
- Em suma, falta tolerância a faltas, propagação e "uso e libertação de leases" (a nivel dos tm)