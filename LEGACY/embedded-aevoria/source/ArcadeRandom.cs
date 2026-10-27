namespace SessionDashboard;
// Explicit PRNG state makes a resumed world follow the same random sequence.
internal sealed class ArcadeRandom : Random
{
    internal ulong State { get; set; }
    internal ArcadeRandom(int? seed=null) {State=seed is {} value ? unchecked((ulong)(uint)value)+1 : unchecked((ulong)DateTime.UtcNow.Ticks);if(State==0)State=1;}
    private ulong Bits() {var x=State;x^=x>>12;x^=x<<25;x^=x>>27;State=x;return x*2685821657736338717UL;}
    public override double NextDouble()=>(Bits()>>11)*(1.0/(1UL<<53));
    protected override double Sample()=>NextDouble();
    public override int Next()=>Next(int.MaxValue);
    public override int Next(int maxValue) {ArgumentOutOfRangeException.ThrowIfNegative(maxValue);return (int)(NextDouble()*maxValue);}
    public override int Next(int minValue,int maxValue) {if(minValue>maxValue)throw new ArgumentOutOfRangeException(nameof(maxValue));return (int)(minValue+NextDouble()*((long)maxValue-minValue));}
    public override void NextBytes(byte[] buffer)=>NextBytes(buffer.AsSpan());
    public override void NextBytes(Span<byte> buffer) {for(var i=0;i<buffer.Length;i++)buffer[i]=(byte)(Bits()>>56);}
}
