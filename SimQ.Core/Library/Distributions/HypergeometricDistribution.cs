namespace SimQCore.Library.Distributions {
    public class HypergeometricDistribution: IDistribution {
        private int N;
        private int n;
        private int K;
        private BernoulliDistribution _bernoulliDistribution;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="N">Кол-во всех элементов</param>
        /// <param name="n">Число «испытаний»</param>
        /// <param name="K">Кол-во интересующих элементов</param>
        public HypergeometricDistribution( int N, int n, int K ) {
            this.N = N;
            this.n = n;
            this.K = K;
            _bernoulliDistribution = new BernoulliDistribution( 0.0 );
        }

        public double Generate() {
            double sum = 0;
            var p = (double)K / N;
            for( int i = 1; i <= n; ++i ) {
                if( _bernoulliDistribution.Generate( p ) == 1.0 && ++sum == K )
                    return sum;
                p = ( K - sum ) / ( N - i );
            }
            return sum;
        }
    }
}
