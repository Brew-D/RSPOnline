public static class RPSEffectiveChecker
{
    public static int Resolve(WeaponType attack, WeaponType defense)
    {
        // 같은 속성이면 0으로 구분
        if (attack == defense) return 0;

        // 공격자가 지는 속성이면 -1
        if (attack == WeaponType.Rock && defense == WeaponType.Scissors) return -1;
        if (attack == WeaponType.Scissors && defense == WeaponType.Paper) return -1;
        if (attack == WeaponType.Paper && defense == WeaponType.Rock) return -1;

        // 그 외 1
        return 1;
    }
}
