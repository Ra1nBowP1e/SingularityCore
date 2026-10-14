namespace VoidMinerProject;

public class ValueTierConfig
{
	public float minValue;

	public float maxValue;

	public int quantityMultiplier;

	public float workHours;

	public ValueTierConfig(float min, float max, int quantity, float hours)
	{
		minValue = min;
		maxValue = max;
		quantityMultiplier = quantity;
		workHours = hours;
	}
}
