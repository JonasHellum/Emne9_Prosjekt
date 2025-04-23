DROP DATABASE IF EXISTS emne9_prosjekt;
CREATE DATABASE emne9_prosjekt;
use emne9_prosjekt;

CREATE USER IF NOT EXISTS 'ga-app'@'localhost' IDENTIFIED BY 'ga-5ecret-%';
CREATE USER IF NOT EXISTS 'ga-app'@'%' IDENTIFIED BY 'ga-5ecret-%';

GRANT ALL privileges ON emne9_prosjekt.* TO 'ga-app'@'%';
GRANT ALL privileges ON emne9_prosjekt.* TO 'ga-app'@'localhost';

FLUSH PRIVILEGES;