namespace Game.Core.Audio
{
    /// <summary>
    /// Id âm thanh ổn định (tách khỏi tên file, docs/03 §2 tinh thần). Catalog gán clip theo các id này.
    /// Thêm id mới thì cập nhật <see cref="All"/>, AudioCatalog.asset và ghi vào Handoff.
    /// </summary>
    public static class AudioIds
    {
        // SFX
        public const string SfxPlayerSwing = "sfx.player_swing";
        public const string SfxHitEnemy = "sfx.hit_enemy";
        public const string SfxHitPlayer = "sfx.hit_player";
        public const string SfxEnemyDie = "sfx.enemy_die";
        public const string SfxTechnique = "sfx.technique";
        public const string SfxDodge = "sfx.dodge";
        public const string SfxPickup = "sfx.pickup";
        public const string SfxLevelUp = "sfx.level_up";
        public const string SfxUiClick = "sfx.ui_click";
        public const string SfxUiBack = "sfx.ui_back";
        public const string SfxPotion = "sfx.potion";
        public const string SfxBossTelegraph = "sfx.boss_telegraph";
        /// <summary>Có trong gói placeholder; chưa ai phát ở P9.5 (dùng từ P9.8 cho quái bắn xa).</summary>
        public const string SfxEnemyShoot = "sfx.enemy_shoot";

        // Nhạc
        public const string MusicGameplay = "music.gameplay";
        public const string MusicBoss = "music.boss";
        /// <summary>Có trong gói placeholder; chưa ai phát ở P9.5 (dùng từ P9.7a cho Thị trấn).</summary>
        public const string MusicTown = "music.town";

        public static readonly string[] All =
        {
            SfxPlayerSwing, SfxHitEnemy, SfxHitPlayer, SfxEnemyDie, SfxTechnique, SfxDodge,
            SfxPickup, SfxLevelUp, SfxUiClick, SfxUiBack, SfxPotion, SfxBossTelegraph, SfxEnemyShoot,
            MusicGameplay, MusicBoss, MusicTown
        };
    }
}
