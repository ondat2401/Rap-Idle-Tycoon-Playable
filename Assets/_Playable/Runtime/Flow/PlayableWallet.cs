using System;

namespace _Playable.Runtime.Flow
{
    /// <summary>
    /// Vi tien cua playable. Plain C# - khong MonoBehaviour, khong ScriptableObject, de de test.
    /// Chi giu so du; phan hien thi va tween do <c>PlayableHudView</c> lo.
    /// </summary>
    public sealed class PlayableWallet
    {
        private readonly long _goal;

        public PlayableWallet(long startMoney, long goal)
        {
            this.Current = startMoney;
            this._goal = goal;
        }

        public long Current { get; private set; }

        public bool IsFull => this.Current >= this._goal;

        /// <summary>Cong tien, chan tran o muc tieu de so hien thi khong vuot 999.999 nhu ban goc.</summary>
        public void Add(long amount)
        {
            if (amount <= 0L)
            {
                return;
            }

            this.Current = Math.Min(this.Current + amount, this._goal);
        }

        /// <summary>Tru tien neu du. Tra ve false khi khong du - caller hien hieu ung bao thieu.</summary>
        public bool TrySpend(long amount)
        {
            if (amount < 0L || this.Current < amount)
            {
                return false;
            }

            this.Current -= amount;
            return true;
        }

        public bool CanAfford(long amount)
        {
            return this.Current >= amount;
        }
    }
}
