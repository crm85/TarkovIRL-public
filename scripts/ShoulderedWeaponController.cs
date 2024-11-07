using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovIRL
{
    internal class ShoulderedWeaponController
    {
        static bool _shoulderSlotFilled = false;
        static bool _weaponOnShoulder = false;

        public static void SetWeaponShouldered(bool shoulderWeapon)
        {
            _weaponOnShoulder = shoulderWeapon;
        }
    }
}
