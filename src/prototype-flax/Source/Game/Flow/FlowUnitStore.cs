using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Flow;

/// <summary>
/// Represents a collection of fluids of different types
/// </summary>
public class FlowUnitStore
{
	private List<FlowUnit> units = new();

	public IReadOnlyList<FlowUnit> Units => units;
	public IEnumerable<long> EnumerableTypes => units.Select(u => u.Type).ToArray();

	public void Clear()
	{
		units.Clear();
	}

	public FlowUnit Get(long type)
	{
		foreach (var unit in units)
			if (unit.Type == type)
				return unit;
		return new(type, 0);
	}

	public void Set(in FlowUnit unit)
	{
		int index = IndexOf(unit.Type);
		if (index >= 0)
			units[index] = unit;
		else
			units.Add(unit);
	}
	public void Set(FlowUnitStore source)
	{
		units.Clear();
		units.AddRange(source.units);
	}

	public void Add(in FlowUnit unit)
	{
		if (unit.Amount == 0)
			return;
		int index = IndexOf(unit.Type);
		if (index >= 0)
			units[index] = new(unit.Type, units[index].Amount + unit.Amount);
		else
			units.Add(unit);
	}
	public void Add(FlowUnitStore source)
	{
		foreach (var unit in source.Units)
			Add(unit);
	}

	public FlowUnit Remove(in FlowUnit unit)
	{
		if (unit.Amount == 0)
			return unit;
		int index = IndexOf(unit.Type);
		long removed = 0;
		if (index >= 0)
		{
			removed = Math.Min(unit.Amount, units[index].Amount);
			if (removed == units[index].Amount)
				units.RemoveAt(index);
			else
				units[index] = new(unit.Type, units[index].Amount - removed);
		}
		return new(unit.Type, removed);
	}

	public long GetTotalAmount()
		=> units.Sum(unit => unit.Amount);

	private int IndexOf(long type)
	{
		for (int i=0; i<units.Count; i++)
			if (units[i].Type == type)
				return i;
		return -1;
	}
}
