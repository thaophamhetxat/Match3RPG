using UnityEngine;

namespace Match3RPG.Core
{
    public sealed class CurrencyService : MonoBehaviour
    {
        static CurrencyService instance;
        public int GoldBalance { get; private set; }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        public static CurrencyService GetOrCreate()
        {
            if (instance != null)
                return instance;
            GameObject serviceObject = new GameObject(nameof(CurrencyService));
            return serviceObject.AddComponent<CurrencyService>();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            GoldBalance = checked(GoldBalance + amount);
        }
    }
}
