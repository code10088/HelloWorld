-- 背包功能增量迁移（不触碰已有邮件/玩家数据）
USE helloworld;

CREATE TABLE IF NOT EXISTS bag_items (
  id      BIGINT UNSIGNED AUTO_INCREMENT,
  user_id VARCHAR(64)     NOT NULL,
  item_id INT UNSIGNED    NOT NULL,
  count   INT UNSIGNED    NOT NULL,
  PRIMARY KEY (id),
  KEY idx_user_item (user_id, item_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS bag_equips (
  uid      BIGINT UNSIGNED AUTO_INCREMENT,
  user_id  VARCHAR(64)     NOT NULL,
  equip_id INT UNSIGNED    NOT NULL,
  level    INT UNSIGNED    NOT NULL DEFAULT 1,
  bind     INT UNSIGNED    NOT NULL DEFAULT 0,
  PRIMARY KEY (uid),
  KEY idx_user (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS player_flags (
  user_id VARCHAR(64) NOT NULL,
  flag    VARCHAR(32) NOT NULL,
  value   INT         NOT NULL DEFAULT 0,
  PRIMARY KEY (user_id, flag)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;