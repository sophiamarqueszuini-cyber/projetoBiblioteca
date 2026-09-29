-- =====================================================================
--  SISTEMA DE BIBLIOTECA - biblioteca_db   (versão 2)
--  Script completo: banco, tabelas, dados iniciais e stored procedures
--  Padrão: exclusão LÓGICA (status = 1 ativo / status = 0 excluído)
--  Novidades v2: autores, gêneros e editoras em tabelas próprias (FK)
--                e empréstimo com VÁRIOS livros (tabela emprestimo_itens)
--  Execute este arquivo inteiro no MySQL Workbench (raio "Execute").
-- =====================================================================

SET NAMES utf8mb4;

DROP DATABASE IF EXISTS biblioteca_db;
CREATE DATABASE biblioteca_db CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE biblioteca_db;

-- ---------------------------------------------------------------------
--  TABELAS
-- ---------------------------------------------------------------------

-- Usuarios do sistema (quem faz login)
CREATE TABLE usuarios (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    nome           VARCHAR(100) NOT NULL,
    login          VARCHAR(50)  NOT NULL UNIQUE,
    senha_hash     CHAR(64)     NOT NULL,          -- SHA-256 em hexadecimal
    perfil         VARCHAR(20)  NOT NULL,          -- Administrador | Gerente | Bibliotecario
    data_cadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status         TINYINT(1)   NOT NULL DEFAULT 1,
    CONSTRAINT ck_usuarios_perfil CHECK (perfil IN ('Administrador','Gerente','Bibliotecario'))
);

-- Leitores (quem pega livros emprestados - NAO fazem login)
CREATE TABLE leitores (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    nome           VARCHAR(100) NOT NULL,
    cpf            VARCHAR(14)  NOT NULL UNIQUE,
    telefone       VARCHAR(20)  NULL,
    email          VARCHAR(100) NULL,
    endereco       VARCHAR(200) NULL,
    data_cadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status         TINYINT(1)   NOT NULL DEFAULT 1
);

-- Tabelas de apoio do acervo (normalizacao)
CREATE TABLE autores (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    nome           VARCHAR(100) NOT NULL UNIQUE,
    data_cadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status         TINYINT(1)   NOT NULL DEFAULT 1
);

CREATE TABLE generos (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    nome           VARCHAR(50)  NOT NULL UNIQUE,
    data_cadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status         TINYINT(1)   NOT NULL DEFAULT 1
);

CREATE TABLE editoras (
    id             INT AUTO_INCREMENT PRIMARY KEY,
    nome           VARCHAR(100) NOT NULL UNIQUE,
    data_cadastro  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status         TINYINT(1)   NOT NULL DEFAULT 1
);

-- Livros (acervo) - autor, genero e editora agora sao chaves estrangeiras
CREATE TABLE livros (
    id                     INT AUTO_INCREMENT PRIMARY KEY,
    titulo                 VARCHAR(150) NOT NULL,
    autor_id               INT          NOT NULL,
    genero_id              INT          NOT NULL,
    editora_id             INT          NOT NULL,
    isbn                   VARCHAR(20)  NULL,
    ano_publicacao         INT          NULL,
    quantidade_total       INT          NOT NULL DEFAULT 1,
    quantidade_disponivel  INT          NOT NULL DEFAULT 1,
    data_cadastro          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status                 TINYINT(1)   NOT NULL DEFAULT 1,
    CONSTRAINT fk_livro_autor   FOREIGN KEY (autor_id)   REFERENCES autores(id),
    CONSTRAINT fk_livro_genero  FOREIGN KEY (genero_id)  REFERENCES generos(id),
    CONSTRAINT fk_livro_editora FOREIGN KEY (editora_id) REFERENCES editoras(id),
    CONSTRAINT ck_livros_qtd CHECK (quantidade_disponivel >= 0 AND quantidade_disponivel <= quantidade_total)
);

-- Emprestimos (CABECALHO): um leitor, um atendente, datas e prazo
CREATE TABLE emprestimos (
    id                       INT AUTO_INCREMENT PRIMARY KEY,
    leitor_id                INT        NOT NULL,
    usuario_id               INT        NOT NULL,          -- quem registrou
    data_emprestimo          DATE       NOT NULL,
    data_prevista_devolucao  DATE       NOT NULL,          -- emprestimo + 7 dias
    data_devolucao           DATE       NULL,              -- preenchida quando o ULTIMO livro volta
    prorrogado               TINYINT(1) NOT NULL DEFAULT 0, -- 1 = ja usou a prorrogacao de +1 dia
    status                   TINYINT(1) NOT NULL DEFAULT 1,
    CONSTRAINT fk_emp_leitor  FOREIGN KEY (leitor_id)  REFERENCES leitores(id),
    CONSTRAINT fk_emp_usuario FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
);

-- Itens do emprestimo: cada livro emprestado (um emprestimo pode ter varios)
CREATE TABLE emprestimo_itens (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    emprestimo_id   INT  NOT NULL,
    livro_id        INT  NOT NULL,
    data_devolucao  DATE NULL,                              -- NULL = este livro ainda nao voltou
    CONSTRAINT fk_item_emprestimo FOREIGN KEY (emprestimo_id) REFERENCES emprestimos(id),
    CONSTRAINT fk_item_livro      FOREIGN KEY (livro_id)      REFERENCES livros(id),
    CONSTRAINT uq_item_livro UNIQUE (emprestimo_id, livro_id) -- o mesmo livro nao entra 2x no mesmo emprestimo
);

-- ---------------------------------------------------------------------
--  DADOS INICIAIS
--  Usuario: admin   Senha: admin123   (troque depois do primeiro acesso)
-- ---------------------------------------------------------------------
INSERT INTO usuarios (nome, login, senha_hash, perfil)
VALUES ('Administrador do Sistema', 'admin', SHA2('admin123', 256), 'Administrador');

INSERT INTO leitores (nome, cpf, telefone, email, endereco) VALUES
('Maria Oliveira', '111.111.111-11', '(11) 91111-1111', 'maria@email.com', 'Rua A, 100'),
('João Santos',    '222.222.222-22', '(11) 92222-2222', 'joao@email.com',  'Rua B, 200');

INSERT INTO autores  (nome) VALUES ('Machado de Assis'), ('Aluísio Azevedo'), ('Robert C. Martin');
INSERT INTO generos  (nome) VALUES ('Romance'), ('Tecnologia'), ('Poesia');
INSERT INTO editoras (nome) VALUES ('Principis'), ('Alta Books');

INSERT INTO livros (titulo, autor_id, genero_id, editora_id, isbn, ano_publicacao, quantidade_total, quantidade_disponivel) VALUES
('Dom Casmurro', 1, 1, 1, '9788594318602', 1899, 3, 3),
('O Cortiço',    2, 1, 1, '9788594318619', 1890, 2, 2),
('Clean Code',   3, 2, 2, '9788576082675', 2008, 1, 1);

DELIMITER $$

-- =====================================================================
--  USUARIOS
-- =====================================================================
CREATE PROCEDURE sp_usuario_login(IN p_login VARCHAR(50), IN p_senha CHAR(64))
BEGIN
    SELECT id, nome, login, perfil
      FROM usuarios
     WHERE login = p_login AND senha_hash = p_senha AND status = 1;
END$$

CREATE PROCEDURE sp_usuario_listar(IN p_status TINYINT)
BEGIN
    SELECT id, nome, login, perfil, data_cadastro, status
      FROM usuarios
     WHERE status = p_status
     ORDER BY nome;
END$$

CREATE PROCEDURE sp_usuario_obter(IN p_id INT)
BEGIN
    SELECT id, nome, login, perfil, data_cadastro, status
      FROM usuarios
     WHERE id = p_id;
END$$

CREATE PROCEDURE sp_usuario_criar(IN p_nome VARCHAR(100), IN p_login VARCHAR(50),
                                  IN p_senha CHAR(64), IN p_perfil VARCHAR(20))
BEGIN
    IF EXISTS (SELECT 1 FROM usuarios WHERE login = p_login) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um usuário com este login.';
    END IF;
    INSERT INTO usuarios (nome, login, senha_hash, perfil)
    VALUES (p_nome, p_login, p_senha, p_perfil);
END$$

-- p_senha NULL = mantem a senha atual
CREATE PROCEDURE sp_usuario_editar(IN p_id INT, IN p_nome VARCHAR(100), IN p_login VARCHAR(50),
                                   IN p_senha CHAR(64), IN p_perfil VARCHAR(20))
BEGIN
    IF EXISTS (SELECT 1 FROM usuarios WHERE login = p_login AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um usuário com este login.';
    END IF;
    UPDATE usuarios
       SET nome       = p_nome,
           login      = p_login,
           perfil     = p_perfil,
           senha_hash = IFNULL(p_senha, senha_hash)
     WHERE id = p_id;
END$$

-- Qualquer usuario logado pode trocar a PROPRIA senha
CREATE PROCEDURE sp_usuario_alterar_senha(IN p_id INT, IN p_senha_atual CHAR(64), IN p_senha_nova CHAR(64))
BEGIN
    IF NOT EXISTS (SELECT 1 FROM usuarios WHERE id = p_id AND senha_hash = p_senha_atual) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Senha atual incorreta.';
    END IF;
    UPDATE usuarios SET senha_hash = p_senha_nova WHERE id = p_id;
END$$

CREATE PROCEDURE sp_usuario_excluir(IN p_id INT)
BEGIN
    UPDATE usuarios SET status = 0 WHERE id = p_id;
END$$

CREATE PROCEDURE sp_usuario_reativar(IN p_id INT)
BEGIN
    UPDATE usuarios SET status = 1 WHERE id = p_id;
END$$

-- =====================================================================
--  LEITORES
-- =====================================================================
CREATE PROCEDURE sp_leitor_listar(IN p_busca VARCHAR(100), IN p_status TINYINT)
BEGIN
    SELECT id, nome, cpf, telefone, email, endereco, data_cadastro, status
      FROM leitores
     WHERE status = p_status
       AND (p_busca IS NULL OR p_busca = ''
            OR nome LIKE CONCAT('%', p_busca, '%')
            OR cpf  LIKE CONCAT('%', p_busca, '%'))
     ORDER BY nome;
END$$

CREATE PROCEDURE sp_leitor_obter(IN p_id INT)
BEGIN
    SELECT id, nome, cpf, telefone, email, endereco, data_cadastro, status
      FROM leitores WHERE id = p_id;
END$$

CREATE PROCEDURE sp_leitor_criar(IN p_nome VARCHAR(100), IN p_cpf VARCHAR(14), IN p_telefone VARCHAR(20),
                                 IN p_email VARCHAR(100), IN p_endereco VARCHAR(200))
BEGIN
    IF EXISTS (SELECT 1 FROM leitores WHERE cpf = p_cpf AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe um leitor com este CPF.';
    END IF;
    IF EXISTS (SELECT 1 FROM leitores WHERE cpf = p_cpf AND status = 0) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este CPF pertence a um leitor excluído. Reative-o na lista de excluídos.';
    END IF;
    INSERT INTO leitores (nome, cpf, telefone, email, endereco)
    VALUES (p_nome, p_cpf, p_telefone, p_email, p_endereco);
END$$

CREATE PROCEDURE sp_leitor_editar(IN p_id INT, IN p_nome VARCHAR(100), IN p_cpf VARCHAR(14), IN p_telefone VARCHAR(20),
                                  IN p_email VARCHAR(100), IN p_endereco VARCHAR(200))
BEGIN
    IF EXISTS (SELECT 1 FROM leitores WHERE cpf = p_cpf AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe outro leitor com este CPF.';
    END IF;
    UPDATE leitores
       SET nome = p_nome, cpf = p_cpf, telefone = p_telefone, email = p_email, endereco = p_endereco
     WHERE id = p_id;
END$$

CREATE PROCEDURE sp_leitor_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1 FROM emprestimos
                WHERE leitor_id = p_id AND data_devolucao IS NULL AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Leitor possui empréstimos em aberto. Registre a devolução antes de excluir.';
    END IF;
    UPDATE leitores SET status = 0 WHERE id = p_id;
END$$

CREATE PROCEDURE sp_leitor_reativar(IN p_id INT)
BEGIN
    UPDATE leitores SET status = 1 WHERE id = p_id;
END$$


-- =====================================================================
--  AUTORES
-- =====================================================================
CREATE PROCEDURE sp_autor_listar(IN p_busca VARCHAR(100), IN p_status TINYINT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.autor_id = x.id AND l.status = 1) AS qtd_livros
      FROM autores x
     WHERE x.status = p_status
       AND (p_busca IS NULL OR p_busca = '' OR x.nome LIKE CONCAT('%', p_busca, '%'))
     ORDER BY x.nome;
END$$

CREATE PROCEDURE sp_autor_obter(IN p_id INT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.autor_id = x.id AND l.status = 1) AS qtd_livros
      FROM autores x WHERE x.id = p_id;
END$$

CREATE PROCEDURE sp_autor_criar(IN p_nome VARCHAR(100))
BEGIN
    IF EXISTS (SELECT 1 FROM autores WHERE nome = p_nome AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Autor já cadastrado(a).';
    END IF;
    IF EXISTS (SELECT 1 FROM autores WHERE nome = p_nome AND status = 0) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Autor existe na lista de excluídos. Reative o cadastro.';
    END IF;
    INSERT INTO autores (nome) VALUES (p_nome);
END$$

CREATE PROCEDURE sp_autor_editar(IN p_id INT, IN p_nome VARCHAR(100))
BEGIN
    IF EXISTS (SELECT 1 FROM autores WHERE nome = p_nome AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe outro cadastro com este nome.';
    END IF;
    UPDATE autores SET nome = p_nome WHERE id = p_id;
END$$

-- Nao exclui se houver livro ATIVO usando este autor
CREATE PROCEDURE sp_autor_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1 FROM livros WHERE autor_id = p_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Existem livros ativos vinculados a este cadastro. Altere ou exclua os livros antes.';
    END IF;
    UPDATE autores SET status = 0 WHERE id = p_id;
END$$

CREATE PROCEDURE sp_autor_reativar(IN p_id INT)
BEGIN
    UPDATE autores SET status = 1 WHERE id = p_id;
END$$

-- =====================================================================
--  GENEROS
-- =====================================================================
CREATE PROCEDURE sp_genero_listar(IN p_busca VARCHAR(100), IN p_status TINYINT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.genero_id = x.id AND l.status = 1) AS qtd_livros
      FROM generos x
     WHERE x.status = p_status
       AND (p_busca IS NULL OR p_busca = '' OR x.nome LIKE CONCAT('%', p_busca, '%'))
     ORDER BY x.nome;
END$$

CREATE PROCEDURE sp_genero_obter(IN p_id INT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.genero_id = x.id AND l.status = 1) AS qtd_livros
      FROM generos x WHERE x.id = p_id;
END$$

CREATE PROCEDURE sp_genero_criar(IN p_nome VARCHAR(50))
BEGIN
    IF EXISTS (SELECT 1 FROM generos WHERE nome = p_nome AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gênero já cadastrado(a).';
    END IF;
    IF EXISTS (SELECT 1 FROM generos WHERE nome = p_nome AND status = 0) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gênero existe na lista de excluídos. Reative o cadastro.';
    END IF;
    INSERT INTO generos (nome) VALUES (p_nome);
END$$

CREATE PROCEDURE sp_genero_editar(IN p_id INT, IN p_nome VARCHAR(50))
BEGIN
    IF EXISTS (SELECT 1 FROM generos WHERE nome = p_nome AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe outro cadastro com este nome.';
    END IF;
    UPDATE generos SET nome = p_nome WHERE id = p_id;
END$$

-- Nao exclui se houver livro ATIVO usando este gênero
CREATE PROCEDURE sp_genero_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1 FROM livros WHERE genero_id = p_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Existem livros ativos vinculados a este cadastro. Altere ou exclua os livros antes.';
    END IF;
    UPDATE generos SET status = 0 WHERE id = p_id;
END$$

CREATE PROCEDURE sp_genero_reativar(IN p_id INT)
BEGIN
    UPDATE generos SET status = 1 WHERE id = p_id;
END$$

-- =====================================================================
--  EDITORAS
-- =====================================================================
CREATE PROCEDURE sp_editora_listar(IN p_busca VARCHAR(100), IN p_status TINYINT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.editora_id = x.id AND l.status = 1) AS qtd_livros
      FROM editoras x
     WHERE x.status = p_status
       AND (p_busca IS NULL OR p_busca = '' OR x.nome LIKE CONCAT('%', p_busca, '%'))
     ORDER BY x.nome;
END$$

CREATE PROCEDURE sp_editora_obter(IN p_id INT)
BEGIN
    SELECT x.id, x.nome, x.data_cadastro, x.status,
           (SELECT COUNT(*) FROM livros l WHERE l.editora_id = x.id AND l.status = 1) AS qtd_livros
      FROM editoras x WHERE x.id = p_id;
END$$

CREATE PROCEDURE sp_editora_criar(IN p_nome VARCHAR(100))
BEGIN
    IF EXISTS (SELECT 1 FROM editoras WHERE nome = p_nome AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Editora já cadastrado(a).';
    END IF;
    IF EXISTS (SELECT 1 FROM editoras WHERE nome = p_nome AND status = 0) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Editora existe na lista de excluídos. Reative o cadastro.';
    END IF;
    INSERT INTO editoras (nome) VALUES (p_nome);
END$$

CREATE PROCEDURE sp_editora_editar(IN p_id INT, IN p_nome VARCHAR(100))
BEGIN
    IF EXISTS (SELECT 1 FROM editoras WHERE nome = p_nome AND id <> p_id) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Já existe outro cadastro com este nome.';
    END IF;
    UPDATE editoras SET nome = p_nome WHERE id = p_id;
END$$

-- Nao exclui se houver livro ATIVO usando esta editora
CREATE PROCEDURE sp_editora_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1 FROM livros WHERE editora_id = p_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Existem livros ativos vinculados a este cadastro. Altere ou exclua os livros antes.';
    END IF;
    UPDATE editoras SET status = 0 WHERE id = p_id;
END$$

CREATE PROCEDURE sp_editora_reativar(IN p_id INT)
BEGIN
    UPDATE editoras SET status = 1 WHERE id = p_id;
END$$
-- =====================================================================
--  LIVROS  (com JOIN em autores, generos e editoras)
-- =====================================================================
CREATE PROCEDURE sp_livro_listar(IN p_busca VARCHAR(100), IN p_status TINYINT)
BEGIN
    SELECT l.id, l.titulo, l.autor_id, a.nome AS autor_nome, l.genero_id, g.nome AS genero_nome,
           l.editora_id, e.nome AS editora_nome, l.isbn, l.ano_publicacao,
           l.quantidade_total, l.quantidade_disponivel, l.data_cadastro, l.status
      FROM livros l
      JOIN autores  a ON a.id = l.autor_id
      JOIN generos  g ON g.id = l.genero_id
      JOIN editoras e ON e.id = l.editora_id
     WHERE l.status = p_status
       AND (p_busca IS NULL OR p_busca = ''
            OR l.titulo LIKE CONCAT('%', p_busca, '%')
            OR a.nome   LIKE CONCAT('%', p_busca, '%')
            OR g.nome   LIKE CONCAT('%', p_busca, '%')
            OR l.isbn   LIKE CONCAT('%', p_busca, '%'))
     ORDER BY l.titulo;
END$$

CREATE PROCEDURE sp_livro_listar_disponiveis()
BEGIN
    SELECT l.id, l.titulo, l.autor_id, a.nome AS autor_nome, l.genero_id, g.nome AS genero_nome,
           l.editora_id, e.nome AS editora_nome, l.isbn, l.ano_publicacao,
           l.quantidade_total, l.quantidade_disponivel, l.data_cadastro, l.status
      FROM livros l
      JOIN autores  a ON a.id = l.autor_id
      JOIN generos  g ON g.id = l.genero_id
      JOIN editoras e ON e.id = l.editora_id
     WHERE l.status = 1 AND l.quantidade_disponivel > 0
     ORDER BY l.titulo;
END$$

CREATE PROCEDURE sp_livro_obter(IN p_id INT)
BEGIN
    SELECT l.id, l.titulo, l.autor_id, a.nome AS autor_nome, l.genero_id, g.nome AS genero_nome,
           l.editora_id, e.nome AS editora_nome, l.isbn, l.ano_publicacao,
           l.quantidade_total, l.quantidade_disponivel, l.data_cadastro, l.status
      FROM livros l
      JOIN autores  a ON a.id = l.autor_id
      JOIN generos  g ON g.id = l.genero_id
      JOIN editoras e ON e.id = l.editora_id
     WHERE l.id = p_id;
END$$

-- Confere se autor, genero e editora existem e estao ativos
CREATE PROCEDURE sp_livro_validar_referencias(IN p_autor_id INT, IN p_genero_id INT, IN p_editora_id INT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM autores WHERE id = p_autor_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Autor inválido ou excluído.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM generos WHERE id = p_genero_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Gênero inválido ou excluído.';
    END IF;
    IF NOT EXISTS (SELECT 1 FROM editoras WHERE id = p_editora_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Editora inválida ou excluída.';
    END IF;
END$$

CREATE PROCEDURE sp_livro_criar(IN p_titulo VARCHAR(150), IN p_autor_id INT, IN p_genero_id INT, IN p_editora_id INT,
                                IN p_isbn VARCHAR(20), IN p_ano INT, IN p_quantidade INT)
BEGIN
    IF p_quantidade < 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'A quantidade deve ser de pelo menos 1 exemplar.';
    END IF;
    CALL sp_livro_validar_referencias(p_autor_id, p_genero_id, p_editora_id);

    INSERT INTO livros (titulo, autor_id, genero_id, editora_id, isbn, ano_publicacao, quantidade_total, quantidade_disponivel)
    VALUES (p_titulo, p_autor_id, p_genero_id, p_editora_id, p_isbn, p_ano, p_quantidade, p_quantidade);
END$$

-- Ao mudar a quantidade total, a disponivel acompanha a diferenca
CREATE PROCEDURE sp_livro_editar(IN p_id INT, IN p_titulo VARCHAR(150), IN p_autor_id INT, IN p_genero_id INT, IN p_editora_id INT,
                                 IN p_isbn VARCHAR(20), IN p_ano INT, IN p_quantidade INT)
BEGIN
    DECLARE v_emprestados INT;
    SELECT quantidade_total - quantidade_disponivel INTO v_emprestados FROM livros WHERE id = p_id;

    IF p_quantidade < v_emprestados OR p_quantidade < 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Quantidade total menor que o número de exemplares emprestados.';
    END IF;
    CALL sp_livro_validar_referencias(p_autor_id, p_genero_id, p_editora_id);

    UPDATE livros
       SET titulo = p_titulo, autor_id = p_autor_id, genero_id = p_genero_id, editora_id = p_editora_id,
           isbn = p_isbn, ano_publicacao = p_ano,
           quantidade_total      = p_quantidade,
           quantidade_disponivel = p_quantidade - v_emprestados
     WHERE id = p_id;
END$$

CREATE PROCEDURE sp_livro_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1
                 FROM emprestimo_itens i
                 JOIN emprestimos e ON e.id = i.emprestimo_id
                WHERE i.livro_id = p_id AND i.data_devolucao IS NULL AND e.status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Livro possui exemplares emprestados. Registre a devolução antes de excluir.';
    END IF;
    UPDATE livros SET status = 0 WHERE id = p_id;
END$$

-- So reativa se autor, genero e editora tambem estiverem ativos
CREATE PROCEDURE sp_livro_reativar(IN p_id INT)
BEGIN
    DECLARE v_autor INT; DECLARE v_genero INT; DECLARE v_editora INT;
    SELECT autor_id, genero_id, editora_id INTO v_autor, v_genero, v_editora FROM livros WHERE id = p_id;
    CALL sp_livro_validar_referencias(v_autor, v_genero, v_editora);
    UPDATE livros SET status = 1 WHERE id = p_id;
END$$

-- =====================================================================
--  EMPRESTIMOS  (cabecalho + itens)
-- =====================================================================
-- p_filtro: 'abertos' | 'atrasados' | 'devolvidos' | 'todos'
CREATE PROCEDURE sp_emprestimo_listar(IN p_filtro VARCHAR(20))
BEGIN
    SELECT e.id, e.leitor_id, l.nome AS leitor_nome, e.usuario_id, u.nome AS usuario_nome,
           e.data_emprestimo, e.data_prevista_devolucao, e.data_devolucao, e.prorrogado, e.status,
           COUNT(i.id)                                        AS qtd_livros,
           SUM(i.data_devolucao IS NULL)                      AS qtd_pendentes,
           GROUP_CONCAT(b.titulo ORDER BY b.titulo SEPARATOR ' | ') AS livros
      FROM emprestimos e
      JOIN leitores l ON l.id = e.leitor_id
      JOIN usuarios u ON u.id = e.usuario_id
      JOIN emprestimo_itens i ON i.emprestimo_id = e.id
      JOIN livros   b ON b.id = i.livro_id
     WHERE e.status = 1
       AND (   p_filtro = 'todos'
            OR (p_filtro = 'abertos'    AND e.data_devolucao IS NULL)
            OR (p_filtro = 'atrasados'  AND e.data_devolucao IS NULL AND e.data_prevista_devolucao < CURDATE())
            OR (p_filtro = 'devolvidos' AND e.data_devolucao IS NOT NULL))
     GROUP BY e.id, l.nome, u.nome
     ORDER BY e.data_devolucao IS NOT NULL, e.data_prevista_devolucao, e.id DESC;
END$$

CREATE PROCEDURE sp_emprestimo_obter(IN p_id INT)
BEGIN
    SELECT e.id, e.leitor_id, l.nome AS leitor_nome, e.usuario_id, u.nome AS usuario_nome,
           e.data_emprestimo, e.data_prevista_devolucao, e.data_devolucao, e.prorrogado, e.status,
           COUNT(i.id)                                        AS qtd_livros,
           SUM(i.data_devolucao IS NULL)                      AS qtd_pendentes,
           GROUP_CONCAT(b.titulo ORDER BY b.titulo SEPARATOR ' | ') AS livros
      FROM emprestimos e
      JOIN leitores l ON l.id = e.leitor_id
      JOIN usuarios u ON u.id = e.usuario_id
      JOIN emprestimo_itens i ON i.emprestimo_id = e.id
      JOIN livros   b ON b.id = i.livro_id
     WHERE e.id = p_id
     GROUP BY e.id, l.nome, u.nome;
END$$

-- Livros de um emprestimo
CREATE PROCEDURE sp_emprestimo_itens_listar(IN p_emprestimo_id INT)
BEGIN
    SELECT i.id, i.emprestimo_id, i.livro_id, b.titulo AS livro_titulo, a.nome AS autor_nome, i.data_devolucao
      FROM emprestimo_itens i
      JOIN livros  b ON b.id = i.livro_id
      JOIN autores a ON a.id = b.autor_id
     WHERE i.emprestimo_id = p_emprestimo_id
     ORDER BY b.titulo;
END$$

-- PASSO 1 do novo emprestimo: cria o cabecalho (devolucao em 7 dias) e devolve o id
CREATE PROCEDURE sp_emprestimo_criar(IN p_leitor_id INT, IN p_usuario_id INT)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM leitores WHERE id = p_leitor_id AND status = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Leitor inválido ou excluído.';
    END IF;
    IF EXISTS (SELECT 1 FROM emprestimos
                WHERE leitor_id = p_leitor_id AND status = 1
                  AND data_devolucao IS NULL AND data_prevista_devolucao < CURDATE()) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Leitor possui empréstimo em atraso. Regularize antes de um novo empréstimo.';
    END IF;

    INSERT INTO emprestimos (leitor_id, usuario_id, data_emprestimo, data_prevista_devolucao)
    VALUES (p_leitor_id, p_usuario_id, CURDATE(), DATE_ADD(CURDATE(), INTERVAL 7 DAY));

    SELECT LAST_INSERT_ID() AS id;
END$$

-- PASSO 2 (repetido para cada livro): adiciona o livro e baixa o estoque
CREATE PROCEDURE sp_emprestimo_item_adicionar(IN p_emprestimo_id INT, IN p_livro_id INT)
BEGIN
    DECLARE v_titulo VARCHAR(150);
    DECLARE v_msg    VARCHAR(255);

    SELECT titulo INTO v_titulo FROM livros WHERE id = p_livro_id;

    IF EXISTS (SELECT 1 FROM emprestimo_itens WHERE emprestimo_id = p_emprestimo_id AND livro_id = p_livro_id) THEN
        SET v_msg = CONCAT('O livro "', v_titulo, '" foi selecionado mais de uma vez.');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM livros WHERE id = p_livro_id AND status = 1 AND quantidade_disponivel > 0) THEN
        SET v_msg = CONCAT('Livro indisponível para empréstimo: ', IFNULL(v_titulo, CONCAT('id ', p_livro_id)));
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_msg;
    END IF;

    INSERT INTO emprestimo_itens (emprestimo_id, livro_id) VALUES (p_emprestimo_id, p_livro_id);
    UPDATE livros SET quantidade_disponivel = quantidade_disponivel - 1 WHERE id = p_livro_id;
END$$

-- Regra: pode prorrogar UMA vez, por mais 1 dia (vale para todos os livros do emprestimo)
CREATE PROCEDURE sp_emprestimo_prorrogar(IN p_id INT)
BEGIN
    DECLARE v_prorrogado TINYINT;
    DECLARE v_devolucao  DATE;
    DECLARE v_prevista   DATE;

    SELECT prorrogado, data_devolucao, data_prevista_devolucao
      INTO v_prorrogado, v_devolucao, v_prevista
      FROM emprestimos WHERE id = p_id AND status = 1;

    IF v_prevista IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Empréstimo não encontrado.';
    END IF;
    IF v_devolucao IS NOT NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este empréstimo já foi devolvido.';
    END IF;
    IF v_prorrogado = 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este empréstimo já foi prorrogado uma vez.';
    END IF;
    IF v_prevista < CURDATE() THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Empréstimo em atraso não pode ser prorrogado. Registre a devolução.';
    END IF;

    UPDATE emprestimos
       SET data_prevista_devolucao = DATE_ADD(data_prevista_devolucao, INTERVAL 1 DAY),
           prorrogado = 1
     WHERE id = p_id;
END$$

-- Devolve UM livro do emprestimo. Se era o ultimo pendente, fecha o emprestimo.
CREATE PROCEDURE sp_emprestimo_item_devolver(IN p_item_id INT)
BEGIN
    DECLARE v_emprestimo INT;
    DECLARE v_livro      INT;
    DECLARE v_devolucao  DATE;

    SELECT i.emprestimo_id, i.livro_id, i.data_devolucao
      INTO v_emprestimo, v_livro, v_devolucao
      FROM emprestimo_itens i
      JOIN emprestimos e ON e.id = i.emprestimo_id
     WHERE i.id = p_item_id AND e.status = 1;

    IF v_emprestimo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Item de empréstimo não encontrado.';
    END IF;
    IF v_devolucao IS NOT NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este livro já foi devolvido.';
    END IF;

    UPDATE emprestimo_itens SET data_devolucao = CURDATE() WHERE id = p_item_id;
    UPDATE livros SET quantidade_disponivel = quantidade_disponivel + 1 WHERE id = v_livro;

    IF NOT EXISTS (SELECT 1 FROM emprestimo_itens WHERE emprestimo_id = v_emprestimo AND data_devolucao IS NULL) THEN
        UPDATE emprestimos SET data_devolucao = CURDATE() WHERE id = v_emprestimo;
    END IF;
END$$

-- Devolve TODOS os livros pendentes do emprestimo de uma vez
CREATE PROCEDURE sp_emprestimo_devolver(IN p_id INT)
BEGIN
    DECLARE v_prevista  DATE;
    DECLARE v_devolucao DATE;

    SELECT data_prevista_devolucao, data_devolucao INTO v_prevista, v_devolucao
      FROM emprestimos WHERE id = p_id AND status = 1;

    IF v_prevista IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Empréstimo não encontrado.';
    END IF;
    IF v_devolucao IS NOT NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este empréstimo já foi devolvido.';
    END IF;

    -- devolve ao estoque cada livro ainda pendente
    UPDATE livros b
      JOIN emprestimo_itens i ON i.livro_id = b.id
       SET b.quantidade_disponivel = b.quantidade_disponivel + 1
     WHERE i.emprestimo_id = p_id AND i.data_devolucao IS NULL;

    UPDATE emprestimo_itens SET data_devolucao = CURDATE()
     WHERE emprestimo_id = p_id AND data_devolucao IS NULL;

    UPDATE emprestimos SET data_devolucao = CURDATE() WHERE id = p_id;
END$$

-- Exclusao logica. Livros ainda nao devolvidos voltam ao estoque.
CREATE PROCEDURE sp_emprestimo_excluir(IN p_id INT)
BEGIN
    IF EXISTS (SELECT 1 FROM emprestimos WHERE id = p_id AND status = 1) THEN
        UPDATE livros b
          JOIN emprestimo_itens i ON i.livro_id = b.id
           SET b.quantidade_disponivel = b.quantidade_disponivel + 1
         WHERE i.emprestimo_id = p_id AND i.data_devolucao IS NULL;

        UPDATE emprestimos SET status = 0 WHERE id = p_id;
    END IF;
END$$

-- =====================================================================
--  PAINEL (tela inicial)
-- =====================================================================
CREATE PROCEDURE sp_dashboard()
BEGIN
    SELECT
        (SELECT COUNT(*) FROM livros   WHERE status = 1)                                   AS total_livros,
        (SELECT IFNULL(SUM(quantidade_disponivel),0) FROM livros WHERE status = 1)         AS exemplares_disponiveis,
        (SELECT COUNT(*) FROM leitores WHERE status = 1)                                   AS total_leitores,
        (SELECT COUNT(*) FROM emprestimos WHERE status = 1 AND data_devolucao IS NULL)     AS emprestimos_abertos,
        (SELECT COUNT(*) FROM emprestimos WHERE status = 1 AND data_devolucao IS NULL
                                           AND data_prevista_devolucao < CURDATE())        AS emprestimos_atrasados;
END$$

DELIMITER ;
