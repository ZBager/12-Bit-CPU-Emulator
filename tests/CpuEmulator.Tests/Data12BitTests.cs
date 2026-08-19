namespace CpuEmulator.Tests
{
	/// <summary>
	/// Data12Bit's setter mask is the single source of 12-bit wraparound in the
	/// whole emulator, so it gets tested directly rather than only through the ALU.
	/// </summary>
	public class Data12BitTests
	{
		[Theory]
		[InlineData(0x000u, 0x000u)]
		[InlineData(0xfffu, 0xfffu)]
		[InlineData(0x1000u, 0x000u)]
		[InlineData(0x1234u, 0x234u)]
		[InlineData(uint.MaxValue, 0xfffu)]
		public void MasksToTwelveBits(uint written, uint expected)
		{
			Data12Bit d = new Data12Bit(written);
			Assert.Equal(expected, d.Val);

			Data12Bit viaSetter = default;
			viaSetter.Val = written;
			Assert.Equal(expected, viaSetter.Val);
		}

		[Fact]
		public void IsAValueType_SoCopiesAreSnapshots()
		{
			// The UI layer depends on this: a copied row does not track later writes.
			Data12Bit original = new Data12Bit(0x111);
			Data12Bit copy = original;
			original.Val = 0x222;

			Assert.Equal(0x111u, copy.Val);
			Assert.Equal(0x222u, original.Val);
		}
	}
}
