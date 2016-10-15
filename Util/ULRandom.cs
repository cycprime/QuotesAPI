using System;

namespace QuotesAPI.Util
{

    //
    // Custom class to return random numbers ranges from 
    // 0 - 18,446,744,073,709,551,615.
    // 
    public static class ULRandom
    {

        // 
        // Using a C# implementation of the PCG random number generator
        // by bgrainger.  
        // 
        // For details, please see https://github.com/bgrainger/PcgRandom.
        //
        private readonly static System.Random _rng = new Pcg.PcgRandom();



        //
        // Accessor to the PCG Random Number Generator.
        // 
        public static System.Random Rng 
        {

            get
            {

                return _rng;

            }

        }



        // 
        // Returns a random unsigned long integer ranges from 
        // min (inclusive) to max (exclusive).
        // 
        // Implementation is based on a suggestion in StackOverflow.
        // For details, please see http://stackoverflow.com/questions/6651554/random-number-in-long-range-is-this-the-way
        // 
        public static ulong NextULong(ulong min, ulong max)
        {

            if (max <= min)
            {

                throw new ArgumentOutOfRangeException("max", 
                    "Max value needs to be graeter than min value!");
            }

            ulong ulRange = (ulong)(max - min);

            ulong ulRandNum = ULRandomNumber();

            while (ulRandNum > ulong.MaxValue - ((ulong.MaxValue % ulRange) + 1) % ulRange) 
            {

                ulRandNum = ULRandomNumber();

            }

            return (ulong)((ulRandNum % ulRange) + min);

        }

        // 
        // Returns a random unsigned long from 0 (inclusive) to 
        // max (exclusive).
        //
        public static ulong NextULong(ulong max)
        {

            return NextULong(0, max);

        }

        // 
        // Returns a random unsigned long, with no range limitation.
        // 
        public static ulong ULRandomNumber()
        {

            byte[] composite = new byte[8];

            Rng.NextBytes(composite);

            ulong randomNumber = (ulong)BitConverter.ToInt64(composite, 0);

            return randomNumber;

        }

    }

}
