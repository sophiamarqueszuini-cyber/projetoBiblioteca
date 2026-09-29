-- ---------------------------------------------------------------------
--  Acervo adicional com livros, autores, generos e editoras REAIS.
--  Execute DEPOIS do biblioteca_db.sql (e do acervo_leitores_exemplo.sql, se usado).
--  Pode rodar mais de uma vez: livros ja existentes (mesmo titulo) sao ignorados.
--  Ano = ano da primeira publicacao da obra. Editora = a que publica a edicao
--  brasileira. ISBN fica em branco (campo opcional).
-- ---------------------------------------------------------------------
USE biblioteca_db;

-- CORRECOES em livros inseridos por acervo_leitores_exemplo.sql -----------
INSERT IGNORE INTO editoras (nome) VALUES ('L&PM Editores'), ('Agir'), ('HarperCollins Brasil');

UPDATE livros SET editora_id = (SELECT id FROM editoras WHERE nome = 'L&PM Editores')
 WHERE titulo = 'Sapiens: Uma Breve História da Humanidade';
UPDATE livros SET editora_id = (SELECT id FROM editoras WHERE nome = 'Agir')
 WHERE titulo = 'O Pequeno Príncipe';
UPDATE livros SET editora_id = (SELECT id FROM editoras WHERE nome = 'HarperCollins Brasil')
 WHERE titulo = 'Assassinato no Expresso do Oriente';
UPDATE livros SET titulo = 'Arquitetura Limpa'
 WHERE titulo = 'Código Limpo: Arquitetura Limpa';

-- AUTORES -------------------------------------------------------------
INSERT IGNORE INTO autores (nome) VALUES
('João Guimarães Rosa'), ('Paulo Coelho'), ('Miguel de Cervantes'), ('Fiódor Dostoiévski'),
('Jane Austen'), ('Frank Herbert'), ('William Gibson'), ('Ray Bradbury'), ('Douglas Adams'),
('John Green'), ('Rick Riordan'), ('Carolina Maria de Jesus'), ('Itamar Vieira Junior'),
('Dan Brown'), ('Arthur Conan Doyle'), ('Martin Fowler'), ('Erich Gamma'), ('Andrew Hunt'),
('Stephen Hawking'), ('Carl Sagan'), ('Vinicius de Moraes'), ('Ziraldo'),
('Monteiro Lobato'), ('Gilberto Freyre');

-- GENEROS -------------------------------------------------------------
INSERT IGNORE INTO generos (nome) VALUES
('Memórias'), ('Ciência'), ('Policial'), ('Juvenil');

-- EDITORAS ------------------------------------------------------------
INSERT IGNORE INTO editoras (nome) VALUES
('Nova Fronteira'), ('Editora 34'), ('Martin Claret'), ('Arqueiro'), ('Intrínseca'),
('Ática'), ('Todavia'), ('Zahar'), ('Novatec'), ('Bookman'), ('Melhoramentos'), ('Global Editora');

-- LIVROS --------------------------------------------------------------
INSERT INTO livros (titulo, autor_id, genero_id, editora_id, ano_publicacao, quantidade_total, quantidade_disponivel)
SELECT v.titulo, a.id, g.id, e.id, v.ano, v.qtd, v.qtd
  FROM (
    SELECT 'Grande Sertão: Veredas' AS titulo, 'João Guimarães Rosa' AS autor, 'Romance' AS genero, 'Nova Fronteira' AS editora, 1956 AS ano, 3 AS qtd
    UNION ALL SELECT 'O Alquimista',                           'Paulo Coelho',            'Romance',           'Rocco',                1988, 4
    UNION ALL SELECT 'Dom Quixote',                            'Miguel de Cervantes',     'Romance',           'Editora 34',           1605, 2
    UNION ALL SELECT 'Crime e Castigo',                        'Fiódor Dostoiévski',      'Romance',           'Editora 34',           1866, 3
    UNION ALL SELECT 'Orgulho e Preconceito',                  'Jane Austen',             'Romance',           'Martin Claret',        1813, 3
    UNION ALL SELECT 'O Hobbit',                               'J. R. R. Tolkien',        'Fantasia',          'HarperCollins Brasil', 1937, 4
    UNION ALL SELECT 'Duna',                                   'Frank Herbert',           'Ficção Científica', 'Aleph',                1965, 3
    UNION ALL SELECT 'Fundação',                               'Isaac Asimov',            'Ficção Científica', 'Aleph',                1951, 3
    UNION ALL SELECT 'Neuromancer',                            'William Gibson',          'Ficção Científica', 'Aleph',                1984, 2
    UNION ALL SELECT 'Fahrenheit 451',                         'Ray Bradbury',            'Distopia',          'Globo Livros',         1953, 3
    UNION ALL SELECT 'O Guia do Mochileiro das Galáxias',      'Douglas Adams',           'Ficção Científica', 'Arqueiro',             1979, 3
    UNION ALL SELECT 'A Culpa É das Estrelas',                 'John Green',              'Juvenil',           'Intrínseca',           2012, 4
    UNION ALL SELECT 'Percy Jackson e o Ladrão de Raios',      'Rick Riordan',            'Fantasia',          'Intrínseca',           2005, 4
    UNION ALL SELECT 'Quarto de Despejo',                      'Carolina Maria de Jesus', 'Memórias',          'Ática',                1960, 3
    UNION ALL SELECT 'Torto Arado',                            'Itamar Vieira Junior',    'Romance',           'Todavia',              2019, 4
    UNION ALL SELECT 'O Código Da Vinci',                      'Dan Brown',               'Suspense',          'Arqueiro',             2003, 3
    UNION ALL SELECT 'Um Estudo em Vermelho',                  'Arthur Conan Doyle',      'Policial',          'Zahar',                1887, 3
    UNION ALL SELECT 'Refatoração',                            'Martin Fowler',           'Tecnologia',        'Novatec',              1999, 2
    UNION ALL SELECT 'Padrões de Projeto',                     'Erich Gamma',             'Tecnologia',        'Bookman',              1994, 2
    UNION ALL SELECT 'O Programador Pragmático',               'Andrew Hunt',             'Tecnologia',        'Bookman',              1999, 2
    UNION ALL SELECT 'Uma Breve História do Tempo',            'Stephen Hawking',         'Ciência',           'Intrínseca',           1988, 3
    UNION ALL SELECT 'Cosmos',                                 'Carl Sagan',              'Ciência',           'Companhia das Letras', 1980, 3
    UNION ALL SELECT 'Antologia Poética',                      'Vinicius de Moraes',      'Poesia',            'Companhia das Letras', 1954, 2
    UNION ALL SELECT 'O Menino Maluquinho',                    'Ziraldo',                 'Infantil',          'Melhoramentos',        1980, 4
    UNION ALL SELECT 'Reinações de Narizinho',                 'Monteiro Lobato',         'Infantil',          'Globo Livros',         1931, 3
    UNION ALL SELECT 'Casa-Grande & Senzala',                  'Gilberto Freyre',         'História',          'Global Editora',       1933, 2
  ) v
  JOIN autores  a ON a.nome = v.autor
  JOIN generos  g ON g.nome = v.genero
  JOIN editoras e ON e.nome = v.editora
 WHERE NOT EXISTS (SELECT 1 FROM livros l WHERE l.titulo = v.titulo);
