SELECT 'tables' AS section;
SHOW TABLES;
SELECT 'players' AS section;
SELECT user_id, player_id, nick_name, level FROM players;
SELECT 'mails' AS section;
SELECT mail_id, status, title FROM mails ORDER BY mail_id;
SELECT 'rewards' AS section;
SELECT mail_id, item_id, item_count FROM mail_rewards;