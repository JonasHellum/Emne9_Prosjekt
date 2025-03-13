DROP DATABASE IF EXISTS emne9_prosjekt;
CREATE DATABASE emne9_prosjekt;
use emne9_prosjekt;

GRANT ALL privileges ON emne9_prosjekt.* TO 'ga-app'@'%';
GRANT ALL privileges ON emne9_prosjekt.* TO 'ga-app'@'localhost';

FLUSH PRIVILEGES;