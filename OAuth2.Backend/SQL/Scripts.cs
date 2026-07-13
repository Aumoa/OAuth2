using SQLMigration;

namespace OAuth2.SQL;

[DatabaseTarget("OAuth2")]
public class Scripts : IScripts
{
    public IEnumerable<IScript> GetScripts()
    {
        yield return new Init();
        yield return new AddClient();
        yield return new AddClientSub();
        yield return new AddEmailVerify();
        yield return new AddClientUserGroup();
        yield return new FixClientUserGroupUniqueConstraint();
        yield return new AddApiKey();
        yield return new ChangeApiKeyToUserKey();
        yield return new AddApiKeyScopeAndClientRestriction();
        yield return new AddClientDefaultScopes();
        yield return new AddOAuthGrant();
        yield return new AddAccountUpdatedAt();
        yield return new AddEmailVerificationExpiration();
    }

    private class Init : IScript
    {
        public string Name => "Init";

        public int InstalledRank => 1;

        public string UpSql => @"
CREATE TABLE `account` (
    `id` VARCHAR(128) NOT NULL PRIMARY KEY,
    `password` VARCHAR(128) NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    INDEX `IDX__account__created_at` (`created_at`)
);

CREATE TABLE `account_claim` (
	`id` BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    `account_id` VARCHAR(128) NOT NULL,
    `name` VARCHAR(128) NOT NULL,
    `value` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `removed_at` DATETIME,
    INDEX `IDX__account_claim__account_id__name__removed_at__created_at` (`account_id`, `name`, `removed_at`, `created_at`)
);

CREATE TABLE `account_role` (
    `account_id` VARCHAR(128) NOT NULL,
    `name` VARCHAR(32) NOT NULL,
    `granted_at` DATETIME NOT NULL,
    `expired_at` DATETIME,
    PRIMARY KEY (`account_id`, `name`)
);
";

        public string DownSql => @"
DROP TABLE `account`;
DROP TABLE `account_claim`;
DROP TABLE `account_role`;
";
    }

    private class AddClient : IScript
    {
        public string Name => "Add_client";

        public int InstalledRank => 2;

        public string UpSql => @"
CREATE TABLE `client` (
	`id` VARCHAR(128) NOT NULL PRIMARY KEY,
    `owner_id` VARCHAR(128) NOT NULL,
    `name` VARCHAR(512) NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `removed_at` DATETIME,
    INDEX `IDX__client__owner_id__removed_at` (`owner_id`, `removed_at`),
    UNIQUE `UNQ__client__owner_id__removed_at__name` (`owner_id`, `removed_at`, `name`)
);

CREATE TABLE `client_claim` (
	`id` BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    `client_id` VARCHAR(128) NOT NULL,
    `name` VARCHAR(128) NOT NULL,
    `value` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `removed_at` DATETIME,
    INDEX `IDX__client_claim__client_id__removed_at__name` (`client_id`, `removed_at`, `name`)
);
";

        public string DownSql => @"
DROP TABLE `client`;
DROP TABLE `client_claim`;
";
    }

    private class AddClientSub : IScript
    {
        public string Name => "Add_account_sub";

        public int InstalledRank => 3;

        public string UpSql => @"
ALTER TABLE `account`
	ADD COLUMN `sub` VARCHAR(128) NOT NULL AFTER `password`,
    ADD UNIQUE `UNQ__account__sub` (`sub`);

DROP TABLE `account_role`;
";

        public string DownSql => @"
ALTER TABLE `account`
	DROP INDEX `UNQ__account__sub`,
    DROP COLUMN `sub`;

CREATE TABLE `account_role` (
    `account_id` varchar(128) NOT NULL,
    `name` varchar(32) NOT NULL,
    `granted_at` datetime NOT NULL,
    `expired_at` datetime DEFAULT NULL,
    PRIMARY KEY (`account_id`,`name`)
);
";
    }

    private class AddEmailVerify : IScript
    {
        public string Name => "Add_email_verify";

        public int InstalledRank => 4;

        public string UpSql => @"
ALTER TABLE `account`
	ADD COLUMN `name` VARCHAR(128) NOT NULL AFTER `sub`,
	ADD COLUMN `email` VARCHAR(128) NOT NULL AFTER `name`,
    ADD COLUMN `verify_code` VARCHAR(128) AFTER `email`,
    ADD UNIQUE `UNQ__account__email` (`email`);
";

        public string DownSql => @"
ALTER TABLE `account`
	DROP INDEX `UNQ__account__email`,
    DROP COLUMN `verify_code`,
    DROP COLUMN `email`,
    DROP COLUMN `name`;
";
    }

    private class AddClientUserGroup : IScript
    {
        public string Name => "Add_client_user_group";

        public int InstalledRank => 5;

        public string UpSql => @"
CREATE TABLE `client_user_group` (
	`id` BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    `client_id` VARCHAR(128) NOT NULL,
    `account_id` VARCHAR(128) NOT NULL,
    `group` VARCHAR(128) NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `removed_at` DATETIME,
    INDEX `IDX__client_id__account_id` (`client_id`, `account_id`),
    UNIQUE `UNQ__client_id__account_id__group` (`client_id`, `account_id`, `group`)
    );
";

        public string DownSql => @"
DROP TABLE `client_user_group`;
";
    }

    private class FixClientUserGroupUniqueConstraint : IScript
    {
        public string Name => "Fix_client_user_group_unique_constraint";

        public int InstalledRank => 6;

        public string UpSql => @"
ALTER TABLE `client_user_group`
    DROP INDEX `UNQ__client_id__account_id__group`;

ALTER TABLE `client_user_group`
    ADD UNIQUE INDEX `UNQ__client_id__account_id__group__removed_at` (`client_id`, `account_id`, `group`, `removed_at`);
";

        public string DownSql => @"
ALTER TABLE `client_user_group`
    DROP INDEX `UNQ__client_id__account_id__group__removed_at`;

ALTER TABLE `client_user_group`
    ADD UNIQUE INDEX `UNQ__client_id__account_id__group` (`client_id`, `account_id`, `group`);
";
    }

    private class AddApiKey : IScript
    {
        public string Name => "Add_api_key";

        public int InstalledRank => 7;

        public string UpSql => @"
CREATE TABLE `client_api_key` (
    `id` BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    `client_id` VARCHAR(128) NOT NULL,
    `name` VARCHAR(256) NOT NULL,
    `key_prefix` VARCHAR(8) NOT NULL,
    `key_hash` TEXT NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `removed_at` DATETIME,
    INDEX `IDX__key_prefix` (`key_prefix`, `removed_at`),
    INDEX `IDX__client_id` (`client_id`, `removed_at`)
);
";

        public string DownSql => @"
DROP TABLE `client_api_key`;
";
    }

    private class ChangeApiKeyToUserKey : IScript
    {
        public string Name => "Change_api_key_to_user_key";

        public int InstalledRank => 8;

        public string UpSql => @"
ALTER TABLE `client_api_key`
    ADD COLUMN `account_id` VARCHAR(128) NOT NULL AFTER `id`,
    DROP INDEX `IDX__client_id`,
    ADD INDEX `IDX__account_id` (`account_id`, `removed_at`);
";

        public string DownSql => @"
ALTER TABLE `client_api_key`
    DROP COLUMN `account_id`,
    DROP INDEX `IDX__account_id`,
    ADD INDEX `IDX__client_id` (`client_id`, `removed_at`);
";
    }

    private class AddApiKeyScopeAndClientRestriction : IScript
    {
        public string Name => "Add_api_key_scope_and_client_restriction";

        public int InstalledRank => 9;

        public string UpSql => @"
ALTER TABLE `client_api_key`
    MODIFY COLUMN `client_id` VARCHAR(128) NULL DEFAULT NULL,
    ADD COLUMN `allowed_client_id` VARCHAR(128) NULL AFTER `client_id`,
    ADD COLUMN `allowed_scope` VARCHAR(512) NULL AFTER `allowed_client_id`;
";

        public string DownSql => @"
ALTER TABLE `client_api_key`
    DROP COLUMN `allowed_client_id`,
    DROP COLUMN `allowed_scope`,
    MODIFY COLUMN `client_id` VARCHAR(128) NOT NULL;
";
    }

    private class AddClientDefaultScopes : IScript
    {
        public string Name => "Add_client_default_scopes";

        public int InstalledRank => 10;

        public string UpSql => @"
INSERT INTO `client_claim` (`client_id`, `name`, `value`)
SELECT `c`.`id`, 'scope', `s`.`scope`
FROM `client` `c`
CROSS JOIN (
    SELECT 'openid' AS `scope`
    UNION ALL SELECT 'profile'
    UNION ALL SELECT 'email'
    UNION ALL SELECT 'address'
    UNION ALL SELECT 'phone'
    UNION ALL SELECT 'groups'
) `s`
LEFT JOIN `client_claim` `cc`
    ON `cc`.`client_id` = `c`.`id`
    AND `cc`.`name` = 'scope'
    AND `cc`.`value` = `s`.`scope`
    AND `cc`.`removed_at` IS NULL
WHERE `c`.`removed_at` IS NULL
    AND `cc`.`id` IS NULL;
";

        public string DownSql => @"
UPDATE `client_claim`
SET `removed_at` = NOW()
WHERE `name` = 'scope'
    AND `value` IN ('openid', 'profile', 'email', 'address', 'phone', 'groups')
    AND `removed_at` IS NULL;
";
    }

    private class AddOAuthGrant : IScript
    {
        public string Name => "Add_oauth_grant";

        public int InstalledRank => 11;

        public string UpSql => @"
CREATE TABLE `oauth_grant` (
    `id` BIGINT AUTO_INCREMENT NOT NULL PRIMARY KEY,
    `account_id` VARCHAR(128) NOT NULL,
    `client_id` VARCHAR(128) NOT NULL,
    `scope` VARCHAR(64) NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT NOW(),
    `updated_at` DATETIME NOT NULL DEFAULT NOW(),
    `revoked_at` DATETIME,
    UNIQUE `UNQ__oauth_grant__account_id__client_id__scope` (`account_id`, `client_id`, `scope`),
    INDEX `IDX__oauth_grant__account_id__client_id__revoked_at` (`account_id`, `client_id`, `revoked_at`)
);
";

        public string DownSql => @"
DROP TABLE `oauth_grant`;
";
    }

    private class AddAccountUpdatedAt : IScript
    {
        public string Name => "Add_account_updated_at";

        public int InstalledRank => 12;

        public string UpSql => @"
ALTER TABLE `account`
    ADD COLUMN `updated_at` DATETIME NULL AFTER `created_at`;

UPDATE `account`
SET `updated_at` = `created_at`
WHERE `updated_at` IS NULL;

ALTER TABLE `account`
    MODIFY COLUMN `updated_at` DATETIME NOT NULL DEFAULT NOW(),
    ADD INDEX `IDX__account__updated_at` (`updated_at`);
";

        public string DownSql => @"
ALTER TABLE `account`
    DROP INDEX `IDX__account__updated_at`,
    DROP COLUMN `updated_at`;
";
    }

    private class AddEmailVerificationExpiration : IScript
    {
        public string Name => "Add_email_verification_expiration";

        public int InstalledRank => 13;

        public string UpSql => @"
ALTER TABLE `account`
    ADD COLUMN `verify_code_expires_at` DATETIME NULL AFTER `verify_code`;

UPDATE `account`
SET `verify_code_expires_at` = DATE_ADD(UTC_TIMESTAMP(), INTERVAL 30 MINUTE)
WHERE `verify_code` IS NOT NULL;
";

        public string DownSql => @"
ALTER TABLE `account`
    DROP COLUMN `verify_code_expires_at`;
";
    }
}
