-- ---------------------------------------------------------------------
--  10 usuarios de exemplo para o sistema (2 Administradores, 3 Gerentes, 5 Bibliotecarios)
--  Execute DEPOIS do biblioteca_db.sql.
--  Senha de todos: senha123   (troque depois do primeiro acesso)
-- ---------------------------------------------------------------------
USE biblioteca_db;

INSERT INTO usuarios (nome, login, senha_hash, perfil) VALUES
('Carlos Eduardo Lima',   'carlos.lima',    SHA2('senha123', 256), 'Administrador'),
('Fernanda Souza',        'fernanda.souza', SHA2('senha123', 256), 'Administrador'),
('Roberto Almeida',       'roberto.almeida',SHA2('senha123', 256), 'Gerente'),
('Juliana Martins',       'juliana.martins',SHA2('senha123', 256), 'Gerente'),
('Marcos Pereira',        'marcos.pereira', SHA2('senha123', 256), 'Gerente'),
('Ana Beatriz Costa',     'ana.costa',      SHA2('senha123', 256), 'Bibliotecario'),
('Lucas Ferreira',        'lucas.ferreira', SHA2('senha123', 256), 'Bibliotecario'),
('Patrícia Rocha',        'patricia.rocha', SHA2('senha123', 256), 'Bibliotecario'),
('Rafael Gomes',          'rafael.gomes',   SHA2('senha123', 256), 'Bibliotecario'),
('Camila Ribeiro',        'camila.ribeiro', SHA2('senha123', 256), 'Bibliotecario');
