CREATE DATABASE IF NOT EXISTS helloworld DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;

CREATE USER IF NOT EXISTS 'hw'@'localhost' IDENTIFIED WITH mysql_native_password BY 'hw123456';
CREATE USER IF NOT EXISTS 'hw'@'%' IDENTIFIED WITH mysql_native_password BY 'hw123456';
GRANT ALL PRIVILEGES ON helloworld.* TO 'hw'@'localhost';
GRANT ALL PRIVILEGES ON helloworld.* TO 'hw'@'%';
FLUSH PRIVILEGES;

USE helloworld;

CREATE TABLE IF NOT EXISTS players (
  user_id       VARCHAR(64)      NOT NULL,
  player_id     BIGINT UNSIGNED  NOT NULL,
  nick_name     VARCHAR(64)      NOT NULL,
  avatar        VARCHAR(64)      NOT NULL,
  level         INT UNSIGNED     NOT NULL DEFAULT 1,
  register_time BIGINT           NOT NULL,
  created_at    TIMESTAMP        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  updated_at    TIMESTAMP        NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS mails (
  id          BIGINT UNSIGNED AUTO_INCREMENT,
  user_id     VARCHAR(64)     NOT NULL,
  mail_id     BIGINT UNSIGNED NOT NULL,
  type        INT             NOT NULL DEFAULT 0,
  status      INT             NOT NULL DEFAULT 0,
  title       VARCHAR(256)    NOT NULL,
  content     TEXT            NOT NULL,
  create_time BIGINT          NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_user_mail (user_id, mail_id),
  KEY idx_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS mail_rewards (
  id         BIGINT UNSIGNED AUTO_INCREMENT,
  user_id    VARCHAR(64)     NOT NULL,
  mail_id    BIGINT UNSIGNED NOT NULL,
  item_id    INT UNSIGNED    NOT NULL,
  item_count INT UNSIGNED    NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_user_mail_item (user_id, mail_id, item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 背包：道具按格子存（客户端没有格子索引，顺序由 id 决定）
CREATE TABLE IF NOT EXISTS bag_items (
  id      BIGINT UNSIGNED AUTO_INCREMENT,
  user_id VARCHAR(64)     NOT NULL,
  item_id INT UNSIGNED    NOT NULL,
  count   INT UNSIGNED    NOT NULL,
  PRIMARY KEY (id),
  KEY idx_user_item (user_id, item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 背包：装备一格一件，uid 就是装备实例 ID（客户端用 int64）
CREATE TABLE IF NOT EXISTS bag_equips (
  uid      BIGINT UNSIGNED AUTO_INCREMENT,
  user_id  VARCHAR(64)     NOT NULL,
  equip_id INT UNSIGNED    NOT NULL,
  level    INT UNSIGNED    NOT NULL DEFAULT 1,
  bind     INT UNSIGNED    NOT NULL DEFAULT 0,
  PRIMARY KEY (uid),
  KEY idx_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 玩家级开关（例如 bag_init：是否已发放过背包演示数据）
CREATE TABLE IF NOT EXISTS player_flags (
  user_id VARCHAR(64) NOT NULL,
  flag    VARCHAR(32) NOT NULL,
  value   INT         NOT NULL DEFAULT 0,
  PRIMARY KEY (user_id, flag)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

INSERT INTO players (user_id, player_id, nick_name, avatar, level, register_time)
VALUES ('test-user-1', 10001, 'TestPlayer', 'avatar_01', 1, UNIX_TIMESTAMP())
ON DUPLICATE KEY UPDATE nick_name = VALUES(nick_name), avatar = VALUES(avatar), level = VALUES(level);

INSERT INTO mails (user_id, mail_id, type, status, title, content, create_time)
VALUES ('test-user-1', 1, 0, 0, '测试邮件1', '欢迎来到游戏', UNIX_TIMESTAMP()),
       ('test-user-1', 2, 0, 0, '测试邮件2', '这是一封测试邮件', UNIX_TIMESTAMP())
ON DUPLICATE KEY UPDATE title = VALUES(title), content = VALUES(content);

INSERT INTO mail_rewards (user_id, mail_id, item_id, item_count)
VALUES ('test-user-1', 1, 1, 10)
ON DUPLICATE KEY UPDATE item_count = VALUES(item_count);