-- ---------------------------------------------------------------------
--  10 leitores + autores, generos, editoras e livros de exemplo.
--  Execute DEPOIS do biblioteca_db.sql. Pode rodar mais de uma vez
--  (INSERT IGNORE evita duplicar autores, generos, editoras e leitores).
--  Os CPFs abaixo sao ficticios. ISBN fica em branco (campo opcional).
-- ---------------------------------------------------------------------
USE biblioteca_db;

-- LEITORES ------------------------------------------------------------
INSERT IGNORE INTO leitores (nome, cpf, telefone, email, endereco) VALUES
('Ana Paula Ribeiro',     '333.333.333-33', '(11) 93333-3333', 'ana.ribeiro@email.com',    'Av. Paulista, 1000'),
('Bruno Carvalho',        '444.444.444-44', '(11) 94444-4444', 'bruno.carvalho@email.com', 'Rua das Flores, 45'),
('Camila Fernandes',      '555.555.555-55', '(21) 95555-5555', 'camila.fernandes@email.com','Rua do Catete, 210'),
('Diego Nascimento',      '666.666.666-66', '(31) 96666-6666', 'diego.nascimento@email.com','Av. Afonso Pena, 380'),
('Eduarda Lopes',         '777.777.777-77', '(41) 97777-7777', 'eduarda.lopes@email.com',  'Rua XV de Novembro, 92'),
('Felipe Araújo',         '888.888.888-88', '(51) 98888-8888', 'felipe.araujo@email.com',  'Rua dos Andradas, 15'),
('Gabriela Monteiro',     '999.999.999-99', '(61) 99999-9999', 'gabriela.monteiro@email.com','SQS 308, Bloco B'),
('Henrique Barbosa',      '123.456.789-09', '(71) 91234-5678', 'henrique.barbosa@email.com','Rua Chile, 60'),
('Isabela Teixeira',      '234.567.890-12', '(81) 92345-6789', 'isabela.teixeira@email.com','Av. Boa Viagem, 700'),
('João Pedro Cardoso',    '345.678.901-23', '(85) 93456-7890', 'joao.cardoso@email.com',   'Rua Barão de Studart, 123');

-- AUTORES -------------------------------------------------------------
INSERT IGNORE INTO autores (nome) VALUES
('Clarice Lispector'), ('Jorge Amado'), ('Graciliano Ramos'), ('Carlos Drummond de Andrade'),
('George Orwell'), ('J. K. Rowling'), ('J. R. R. Tolkien'), ('Agatha Christie'),
('Isaac Asimov'), ('Yuval Noah Harari'), ('Eric Evans'), ('Antoine de Saint-Exupéry');

-- GENEROS -------------------------------------------------------------
INSERT IGNORE INTO generos (nome) VALUES
('Ficção Científica'), ('Fantasia'), ('Suspense'), ('História'), ('Infantil'), ('Distopia');

-- EDITORAS ------------------------------------------------------------
INSERT IGNORE INTO editoras (nome) VALUES
('Companhia das Letras'), ('Rocco'), ('Record'), ('Martins Fontes'),
('Aleph'), ('Globo Livros'), ('Casa da Palavra');

-- LIVROS --------------------------------------------------------------
INSERT INTO livros (titulo, autor_id, genero_id, editora_id, ano_publicacao, quantidade_total, quantidade_disponivel) VALUES
('A Hora da Estrela',
    (SELECT id FROM autores WHERE nome = 'Clarice Lispector'),
    (SELECT id FROM generos WHERE nome = 'Romance'),
    (SELECT id FROM editoras WHERE nome = 'Rocco'), 1977, 3, 3),
('Capitães da Areia',
    (SELECT id FROM autores WHERE nome = 'Jorge Amado'),
    (SELECT id FROM generos WHERE nome = 'Romance'),
    (SELECT id FROM editoras WHERE nome = 'Companhia das Letras'), 1937, 4, 4),
('Vidas Secas',
    (SELECT id FROM autores WHERE nome = 'Graciliano Ramos'),
    (SELECT id FROM generos WHERE nome = 'Romance'),
    (SELECT id FROM editoras WHERE nome = 'Record'), 1938, 3, 3),
('A Rosa do Povo',
    (SELECT id FROM autores WHERE nome = 'Carlos Drummond de Andrade'),
    (SELECT id FROM generos WHERE nome = 'Poesia'),
    (SELECT id FROM editoras WHERE nome = 'Companhia das Letras'), 1945, 2, 2),
('1984',
    (SELECT id FROM autores WHERE nome = 'George Orwell'),
    (SELECT id FROM generos WHERE nome = 'Distopia'),
    (SELECT id FROM editoras WHERE nome = 'Companhia das Letras'), 1949, 5, 5),
('A Revolução dos Bichos',
    (SELECT id FROM autores WHERE nome = 'George Orwell'),
    (SELECT id FROM generos WHERE nome = 'Distopia'),
    (SELECT id FROM editoras WHERE nome = 'Companhia das Letras'), 1945, 3, 3),
('Harry Potter e a Pedra Filosofal',
    (SELECT id FROM autores WHERE nome = 'J. K. Rowling'),
    (SELECT id FROM generos WHERE nome = 'Fantasia'),
    (SELECT id FROM editoras WHERE nome = 'Rocco'), 1997, 6, 6),
('O Senhor dos Anéis: A Sociedade do Anel',
    (SELECT id FROM autores WHERE nome = 'J. R. R. Tolkien'),
    (SELECT id FROM generos WHERE nome = 'Fantasia'),
    (SELECT id FROM editoras WHERE nome = 'Martins Fontes'), 1954, 4, 4),
('Assassinato no Expresso do Oriente',
    (SELECT id FROM autores WHERE nome = 'Agatha Christie'),
    (SELECT id FROM generos WHERE nome = 'Suspense'),
    (SELECT id FROM editoras WHERE nome = 'Globo Livros'), 1934, 3, 3),
('Eu, Robô',
    (SELECT id FROM autores WHERE nome = 'Isaac Asimov'),
    (SELECT id FROM generos WHERE nome = 'Ficção Científica'),
    (SELECT id FROM editoras WHERE nome = 'Aleph'), 1950, 3, 3),
('Sapiens: Uma Breve História da Humanidade',
    (SELECT id FROM autores WHERE nome = 'Yuval Noah Harari'),
    (SELECT id FROM generos WHERE nome = 'História'),
    (SELECT id FROM editoras WHERE nome = 'Casa da Palavra'), 2011, 4, 4),
('Domain-Driven Design',
    (SELECT id FROM autores WHERE nome = 'Eric Evans'),
    (SELECT id FROM generos WHERE nome = 'Tecnologia'),
    (SELECT id FROM editoras WHERE nome = 'Alta Books'), 2003, 2, 2),
('O Pequeno Príncipe',
    (SELECT id FROM autores WHERE nome = 'Antoine de Saint-Exupéry'),
    (SELECT id FROM generos WHERE nome = 'Infantil'),
    (SELECT id FROM editoras WHERE nome = 'Record'), 1943, 5, 5),
('Memórias Póstumas de Brás Cubas',
    (SELECT id FROM autores WHERE nome = 'Machado de Assis'),
    (SELECT id FROM generos WHERE nome = 'Romance'),
    (SELECT id FROM editoras WHERE nome = 'Principis'), 1881, 3, 3),
('Código Limpo: Arquitetura Limpa',
    (SELECT id FROM autores WHERE nome = 'Robert C. Martin'),
    (SELECT id FROM generos WHERE nome = 'Tecnologia'),
    (SELECT id FROM editoras WHERE nome = 'Alta Books'), 2017, 2, 2);
