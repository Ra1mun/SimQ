namespace SimQCore.Library.TestsGenerators {
    public class LCG {
        private const long a = 1664525; // Множитель, где 0 <= a < m
        private const long c = 1013904223; // Приращение, где 0 <= с < m
        private const long m = 4294967296; // Модуль, где 0 < m (2^32)
        private long ni; // Случайная величина

        /**
         * Число тактов от текущей даты
         */
        public LCG( long seed ) {
            ni = seed;
        }

        /**
         * Получение случайной величины
         */
        public long Next() {
            ni = ( ( a * ni ) + c ) % m;
            return ni;
        }

        /**
         *  Получение случайной величины с учетом максимального значения
         */
        public long Next( long maxValue ) {
            return Next() % maxValue;
        }
    }
}
