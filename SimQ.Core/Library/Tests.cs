using SimQCore.Library.TestsGenerators;
using System;
using System.Diagnostics;

namespace SimQCore.Library {
    public class Tests {
        public static void TestTimeGeneration1() {
            var max = 1000000;
            var timer = new Stopwatch();

            timer.Start();
            var rnd = new Random();
            for( int i = 0; i < max; i++ ) {
                rnd.Next();
            }
            timer.Stop();
            Console.WriteLine( "Random - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            var lcg = new LCG(0);
            for( uint i = 0; i < max; i++ ) {
                lcg.Next();
            }
            timer.Stop();
            Console.WriteLine( "LCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            var xorshift = new Xorshift(0);
            for( uint i = 0; i < max; i++ ) {
                xorshift.Next();
            }
            timer.Stop();
            Console.WriteLine( "XorShift - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            var pcg = new PCG(0);
            for( uint i = 0; i < max; i++ ) {
                pcg.Next( 0, 1 );
            }
            timer.Stop();
            Console.WriteLine( "PCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            var mersenneTwister = new MersenneTwister(0);
            for( uint i = 0; i < max; i++ ) {
                mersenneTwister.Next( 0, 1 );
            }
            timer.Stop();
            Console.WriteLine( "MersenneTwister - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            var bs = new BaseSensor();
            for( uint i = 0; i < max; i++ ) {
                bs.Next();
            }
            timer.Stop();
            Console.WriteLine( "xxHash - " + timer.Elapsed.Milliseconds );
        }

        public static void TestTimeGeneration2() {
            var max = 1000000;
            var timer = new Stopwatch();

            timer.Start();
            for( int i = 0; i < max; i++ ) {
                var rnd = new Random();
                for( int j = 0; j < 100; j++ ) {
                    rnd.Next();
                }
            }
            timer.Stop();
            Console.WriteLine( "Random - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var lcg = new LCG(i);
                for( int j = 0; j < 100; j++ ) {
                    lcg.Next();
                }
            }
            timer.Stop();
            Console.WriteLine( "LCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var xorshift = new Xorshift((int)i);
                for( int j = 0; j < 100; j++ ) {
                    xorshift.Next();
                }
            }
            timer.Stop();
            Console.WriteLine( "XorShift - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var pcg = new PCG(i);
                for( int j = 0; j < 100; j++ ) {
                    pcg.Next( 0, 1 );
                }
            }
            timer.Stop();
            Console.WriteLine( "PCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var mersenneTwister = new MersenneTwister((int)i);
                for( int j = 0; j < 100; j++ ) {
                    mersenneTwister.Next( 0, 1 );
                }
            }
            timer.Stop();
            Console.WriteLine( "MersenneTwister - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var bs = new BaseSensor();
                bs.Next( 100 );
            }
            timer.Stop();
            Console.WriteLine( "xxHash - " + timer.Elapsed.Milliseconds );
        }

        public static void TestTimeGeneration3() {
            var max = 1000000;
            var timer = new Stopwatch();

            timer.Start();
            for( int i = 0; i < max; i++ ) {
                var rnd = new Random();
            }
            timer.Stop();
            Console.WriteLine( "Random - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var lcg = new LCG(i);
            }
            timer.Stop();
            Console.WriteLine( "LCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var xorshift = new Xorshift((int)i);
            }
            timer.Stop();
            Console.WriteLine( "XorShift - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var pcg = new PCG(0);
            }
            timer.Stop();
            Console.WriteLine( "PCG - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var mersenneTwister = new MersenneTwister((int)i);
            }
            timer.Stop();
            Console.WriteLine( "MersenneTwister - " + timer.Elapsed.Milliseconds );

            timer.Reset();
            timer.Start();
            for( uint i = 0; i < max; i++ ) {
                var bs = new BaseSensor(i);
            }
            timer.Stop();
            Console.WriteLine( "xxHash - " + timer.Elapsed.Milliseconds );
        }

        public static void TestBaseSensor1() {

        }

        public static void TestBaseSensor2() {
            var n = 1000;
            Console.WriteLine( "Кол-во случайных чисел, сгенерированных базовым датчиком: " + n );
            var k = Math.Truncate(1 + 3.3 * Math.Log10(n));
            Console.WriteLine( "Кол-во интервалов: " + k );
            var estimatedResult = n / k;
            Console.Write( "Предполагаемый результат: " );
            for( int i = 0; i < k; i++ ) {
                if( i != k - 1 )
                    Console.Write( estimatedResult + ", " );
                else
                    Console.WriteLine( estimatedResult );
            }
            Console.Write( "Фактический результат: " );
            double[][] mat = new double[(int)k][];
            for( int i = 0; i < k; i++ ) {
                mat [i] = new double [n];
            }
            var bs = new BaseSensor();
            for( uint i = 0; i < n; i++ ) {
                var val = bs.Next();
                for( int j = 0; j < n; j++ ) {
                    if( mat [( int )( val * k )] [j] == 0 ) {
                        mat [( int )( val * k )] [j] = val;
                        break;
                    }
                }
            }
            var factResult = new int[(int)k];
            for( uint i = 0; i < k; i++ ) {
                var count = 0;
                for( int j = 0; j < n; j++ ) {
                    if( mat [i] [j] != 0 ) {
                        count++;
                    } else {
                        if( i != k - 1 )
                            Console.Write( count + ", " );
                        else
                            Console.WriteLine( count );
                        factResult [i] = count;
                        break;
                    }
                }
            }
            Console.Write( "Хи-квадрат фактический = " );
            double xi = 0;
            for( int i = 0; i < k; i++ ) {
                xi += ( estimatedResult - factResult [i] ) * ( estimatedResult - factResult [i] ) / factResult [i];
            }
            Console.WriteLine( xi );
        }

        public static void TestBaseSensor3() {
            uint N = 1000;
            Console.WriteLine( "Кол-во случайных чисел, сгенерированных базовым датчиком: " + N );
            var bs = new BaseSensor();
            var k = N / 2 - 2;
            Console.WriteLine( "k = " + k + " < N/2" );
            var arr = new double[N];
            var arrK = new double[k];
            var arrT = new double[k];
            for( uint i = 0; i < N; i++ ) {
                arr [i] = bs.Next( 0, 1 );
            }
            bool check = false;
            for( int i = 1; i < k + 1; i++ ) {
                double sum = 0;
                for( int j = 0; j < N - i; j++ ) {
                    sum += ( arr [j] - 0.5 ) * ( arr [j + i] - 0.5 );
                }

                arrK [i - 1] = sum * ( 12.0 / ( N - k ) );
                arrT [i - 1] = arrK [i - 1] / Math.Sqrt( 1 - arrK [i - 1] * arrK [i - 1] ) * Math.Sqrt( N - i - 2 );
                check = arrT [i - 1] > 1.96;
            }

            Console.Write( "Первые 10 коэффициентов корреляции: " );
            for( int i = 0; i < 10; i++ ) {
                if( i != 9 )
                    Console.Write( Math.Abs( arrK [i] ) + ", " );
                else
                    Console.WriteLine( Math.Abs( arrK [i] ) );
            }
            Console.Write( "Первые 10 значений t-Критерия Стьюдента: " );
            for( int i = 0; i < 10; i++ ) {
                if( i != 9 )
                    Console.Write( Math.Abs( arrT [i] ) + ", " );
                else
                    Console.WriteLine( Math.Abs( arrT [i] ) );
            }
            if( check )
                Console.WriteLine( "Проверка не пройдена" );
            else
                Console.WriteLine( "Проверка пройдена" );
        }
    }
}
