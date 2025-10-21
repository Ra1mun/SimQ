using System;

namespace SimQCore.Library.Distributions {
    public class GammaDistribution: IDistribution {
        delegate double SelectedMethod( double k );

        private BaseSensor _baseSensor;
        private double _k;
        private double _theta;
        private ExponentialDistribution _exponentialDistribution;
        private NormalDistribution _normalDistribution;
        private SelectedMethod _selectedMethod;
        private double b;
        private double d;
        private double m;
        private double s;
        private double w;
        private double v;
        private double c;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rate">Среднее количество успешных испытаний в заданной области возможных исходов</param>
        public GammaDistribution( double k, double theta = 1 ) {
            _baseSensor = new BaseSensor();
            _exponentialDistribution = new ExponentialDistribution( 0.0 );
            _normalDistribution = new NormalDistribution( 0, 0 );
            _k = k;
            _theta = theta;

            if( Math.Floor( _k ) == _k && _k < 5 )
                _selectedMethod = GA1;
            else if( Math.Floor( _k * 2 ) == _k * 2 && _k < 5 )
                _selectedMethod = GA2;
            else if( _k < 1 )
                _selectedMethod = GS;
            else if( k > 1 && k < 3 )
                _selectedMethod = GF;
            else if( k > 3 ) {
                m = k - 1;
                var s_2 = Math.Sqrt(8.0 * k / 3) + k;
                s = Math.Sqrt( s_2 );
                d = Math.Sqrt( 2 ) * Math.Sqrt( 2 ) * s_2;
                b = d + m;
                w = s_2 / ( m - 1 );
                v = ( s_2 + s_2 ) / ( m * Math.Sqrt( k ) );
                c = b + Math.Log( s * d / b ) - m - m - 3.7203285;
                _selectedMethod = GO;
            }
        }

        public double Generate() {
            return _selectedMethod( _k ) * _theta;
        }

        private double GA1( double k ) {
            double x = 0;
            for( int i = 0; i < k; ++i )
                x += _exponentialDistribution.Generate( 1 );
            return x;
        }

        private double GA2( double k ) {
            double x = _normalDistribution.Generate(0, 1);
            x *= 0.5 * x;
            for( int i = 1; i < k; ++i )
                x += _exponentialDistribution.Generate( 1 );
            return x;
        }

        private double GS( double k ) {
            // Assume that k < 1
            double x = 0;
            int iter = 0;
            do {
                // M_E is base of natural logarithm
                double U = _baseSensor.Next(0, 1 + k / Math.E);
                double W = _exponentialDistribution.Generate(1);
                if( U <= 1 ) {
                    x = Math.Pow( U, 1.0 / k );
                    if( x <= W )
                        return x;
                } else {
                    x = -Math.Log( ( 1 - U ) / k + 1.0 / Math.E );
                    if( ( 1 - k ) * Math.Log( x ) <= W )
                        return x;
                }
            } while( ++iter < 1e9 ); // excessive maximum number of rejections
            return double.NaN; // shouldn't end up here
        }

        private double GF( double k ) {
            // Assume that 1 < k < 3
            double E1, E2;
            do {
                E1 = _exponentialDistribution.Generate( 1 );
                E2 = _exponentialDistribution.Generate( 1 );
            } while( E2 < ( k - 1 ) * ( E1 - Math.Log( E1 ) - 1 ) );
            return k * E1;
        }

        double GO( double k ) {
            // Assume that k > 3
            double x = 0;
            int iter = 0;
            do {
                double U = _baseSensor.Next(0, 1);
                if( U <= 0.0095722652 ) {
                    double E1 = _exponentialDistribution.Generate(1);
                    double E2 = _exponentialDistribution.Generate(1);
                    x = b * ( 1 + E1 / d );
                    if( m * ( x / b - Math.Log( x / m ) ) + c <= E2 )
                        return x;
                } else {
                    double N;
                    do {
                        N = _normalDistribution.Generate( 0, 1 );
                        x = s * N + m; // ~ Normal(m, s)
                    } while( x < 0 || x > b );
                    U = _baseSensor.Next( 0, 1 );
                    double S = 0.5 * N * N;
                    if( N > 0 ) {
                        if( U < 1 - w * S )
                            return x;
                    } else if( U < 1 + S * ( v * N - w ) )
                        return x;
                    if( Math.Log( U ) < m * Math.Log( x / m ) + m - x + S )
                        return x;
                }
            } while( ++iter < 1e9 );
            return double.NaN; // shouldn't end up here;
        }
    }
}
