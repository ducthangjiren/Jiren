static void set_aim(void *player, Quaternion look) {
    void (*_set_aim)(void *, Quaternion, bool) = (void (*)(void *, Quaternion, bool))getRealOffset(ENCRYPTOFFSET("0x1051468F0"));//da thay
    _set_aim(player, look, true);
}

void HandleAimbot() {
    if (Aimbot) {
        void *currentMatch = Curent_Match();
        if (!currentMatch) return;  

        void *localPlayer = GetLocalPlayer(currentMatch);
        if (!localPlayer) return; 

        void *closestEnemy = GetClosestEnemy();
        if (!closestEnemy) return;

        Vector3 enemyLocation;
        if (AimWhen2 == 0) {
            enemyLocation = GetHeadPosition(closestEnemy);
        } else if (AimWhen2 == 1) {
            enemyLocation = GetNeckPosition(closestEnemy);
        } else if (AimWhen2 == 2) {
            enemyLocation = GetHipPosition(closestEnemy);
        }

        Vector3 playerLocation = CameraMain(localPlayer);
        if (playerLocation == Vector3(0, 0, 0)) return;

        Quaternion playerLook = GetRotationToTheLocation(enemyLocation, 0.1f, playerLocation);
        bool isScopeOn = get_IsSighting(localPlayer);
        bool isFiring = get_IsFiring(localPlayer);

        if (AimWhen == 0) {
            set_aim(localPlayer, playerLook);
        } else if (AimWhen == 1 && isFiring) {
            set_aim(localPlayer, playerLook);
        } else if (AimWhen == 2 && isScopeOn) {
            set_aim(localPlayer, playerLook);
        } else if (AimWhen == 3 && (isScopeOn || isFiring)) {
            set_aim(localPlayer, playerLook);
        }
        // void *WeaponHand = GetWeaponOnHand(GameFacadeCurrentLocalPlayer());
        // if (isaimkill && WeaponHand != NULL && isVisible(closestEnemy)) {
        // StartAimKillSend(closestEnemy);
        // // StartonFiring(localPlayer, WeaponHand);
        

        // }
    }
}