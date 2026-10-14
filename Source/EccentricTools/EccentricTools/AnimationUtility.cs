namespace EccentricTools;

public static class AnimationUtility
{
	public static float EaseInOutCubic(float value, float start = 0f, float end = 1f)
	{
		value /= 0.5f;
		end -= start;
		if (value < 1f)
		{
			return end * 0.5f * value * value * value + start;
		}
		value -= 2f;
		return end * 0.5f * (value * value * value + 2f) + start;
	}
}
