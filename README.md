# .net

.net开源项目，一些基础类库项目

# 运行环境初始化

> 进行功能验证时，需要使用到一些三方服务；可使用提供的 `Compose.yaml` 进行一键部署

1. 基础服务：redis、rabbitmq
2. 数据库：mongodb、mysql、postgresql、elasticsearch

## 1. Docker部署

- 执行如下命令：`docker compose  -f Compose.yaml up -d`
- 初次启动时，推荐 `docker compose  -f Compose.yaml up` 更方便的查看部署状态和日志信息
  ```shell
  # 上载项目：自动创建服务容器，启动容器
  docker compose  -f Compose.yaml up
  # 下载项目：停止服务容器，删除容器、数据、网络等
  docker compose  -f Compose.yaml down
  # 停止项目：停止服务容器
  docker compose  -f Compose.yaml stop
  # 启动项目：启动服务容器，确保容器已存在
  docker compose  -f Compose.yaml start

  ```

## 2. 数据库初始化

> 进行数据库测试时，需要先初始化对应数据库下的数据表

### 1. ElasticSearch

> 下面语句为 Kibana 中执行，需要在 `Compose.yaml`  在解注释 Kibana 服务
>
> 注意需要先在 ElasticSearch 服务重置 `kibana_system` 密码，再解注释

```bash
DELETE snail_testroutingmodel
PUT snail_testroutingmodel
{
    "mappings": {
    "dynamic": false,
    "_routing": {"required": true}, 
    "properties": {
        "Id":{"type": "keyword"},
        "String":{"type": "wildcard"},
        "Name":{"type": "wildcard"}
    }
    },
    "settings": {
    "index.refresh_interval": "200ms"
    }
}
DELETE snail_testmodel
PUT snail_testmodel
{
    "mappings": {
    "dynamic": false,
    "properties": {
        "Id":{"type": "keyword"},
        "String":{"type": "wildcard"},
        "ParentName":{"type": "wildcard"},
        "Special":{"type": "wildcard"},
        "IntValue":{"type": "integer"},
        "IntNull":{"type": "integer"},
        "Bool":{"type": "boolean"},
        "BoolNull":{"type": "boolean"},
        "Char":{"type": "keyword"},
        "CharNull":{"type": "keyword"},
        "DateTime":{"type": "date"},
        "DateTimeNull":{"type": "date"},
        "NodeType":{"type": "integer"},
        "NodeTypeNull":{"type": "integer"},
        "MyOverride":{"type": "wildcard"}
    }
    },
    "settings": {
    "index.refresh_interval": "200ms"
    }
}
```

### 2. MySql

- 新建数据库：`Test` 选择 `utf8mb4` 作为字符集
- 建表语句：

```sql
-- 数据库名称 切换
use Test;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ----------------------------
-- Table structure for Snail_TestRoutingModel
-- ----------------------------
DROP TABLE IF EXISTS `Snail_TestRoutingModel`;
CREATE TABLE `Snail_TestRoutingModel`  (
    `Id` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `Routing` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `Name` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

-- ----------------------------
-- Table structure for Snail_TestModel
-- ----------------------------
DROP TABLE IF EXISTS `Snail_TestModel`;
CREATE TABLE `Snail_TestModel`  (
    `Id` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `String` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `Special` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `IntValue` int(255) NOT NULL,
    `IntNull` int(255) NULL DEFAULT NULL,
    `Bool` bit(1) NOT NULL,
    `BoolNull` bit(1) NULL DEFAULT NULL,
    `Char` char(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL,
    `CharNull` char(1) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `DateTime` datetime NOT NULL,
    `DateTimeNull` datetime NULL DEFAULT NULL,
    `NodeType` int(255) NOT NULL,
    `NodeTypeNull` int(255) NULL DEFAULT NULL,
    `ParentName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    `MyOverride` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NULL DEFAULT NULL,
    PRIMARY KEY (`Id`) USING BTREE
) ENGINE = InnoDB CHARACTER SET = utf8mb4 COLLATE = utf8mb4_general_ci ROW_FORMAT = Dynamic;

SET FOREIGN_KEY_CHECKS = 1;
```

### 3. Postgresql

- 新建数据库：Test
- 建表语句：

```sql
DROP TABLE IF EXISTS "public"."Snail_TestModel";
CREATE TABLE "public"."Snail_TestModel" (
  "Id" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
  "String" varchar(255) COLLATE "pg_catalog"."default",
  "ParentName" varchar(255) COLLATE "pg_catalog"."default",
  "Special" varchar(255) COLLATE "pg_catalog"."default",
  "IntValue" int4 NOT NULL,
  "IntNull" int4,
  "Char" char(1) COLLATE "pg_catalog"."default" NOT NULL,
  "CharNull" char(1) COLLATE "pg_catalog"."default",
  "DateTime" date NOT NULL,
  "DateTimeNull" date,
  "NodeType" int4 NOT NULL,
  "NodeTypeNull" int4,
  "MyOverride" varchar(255) COLLATE "pg_catalog"."default",
  "Bool" bool NOT NULL,
  "BoolNull" bool
)
;
-- ----------------------------
-- Primary Key structure for table Snail_TestModel
-- ----------------------------
ALTER TABLE "public"."Snail_TestModel" ADD CONSTRAINT "snail_testmodel_pkey" PRIMARY KEY ("Id");


-- ----------------------------
-- Table structure for Snail_TestRoutingModel
-- ----------------------------
DROP TABLE IF EXISTS "public"."Snail_TestRoutingModel";
CREATE TABLE "public"."Snail_TestRoutingModel" (
  "Id" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
  "Routing" varchar(255) COLLATE "pg_catalog"."default" NOT NULL,
  "Name" varchar(255) COLLATE "pg_catalog"."default"
)
;

-- ----------------------------
-- Primary Key structure for table Snail_TestRoutingModel
-- ----------------------------
ALTER TABLE "public"."Snail_TestRoutingModel" ADD CONSTRAINT "Snail_TestRoutingModel_pkey" PRIMARY KEY ("Id");
```
