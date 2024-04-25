using System.ComponentModel.DataAnnotations;

namespace ProgressSystem.Core.Interfaces;

public interface IProgressable
{
    [Range(0, 1)]
    float Progress { get; }
    float ProgressWeight => 1;
    IEnumerable<IProgressable> ProgressChildren => [];
    float GetProgressOfChildren()
    {
        float now = 0, total = 0;
        foreach (var child in ProgressChildren)
        {
            now += child.ProgressWeight * child.Progress;
            total += child.ProgressWeight;
        }
        if (total == 0)
        {
            return 1f;
        }
        return now / total;
    }
    float GetProgressOfChildrenWithProgressHandler(Func<float, float> progressHandler)
    {
        float now = 0, total = 0;
        foreach (var child in ProgressChildren)
        {
            now += child.ProgressWeight * progressHandler(child.Progress);
            total += child.ProgressWeight;
        }
        if (total == 0)
        {
            return 1f;
        }
        return now / total;
    }
    float GetProgressOfChildrenWithWeightHandler(Func<float, float> weightHandler)
    {
        float now = 0, total = 0;
        foreach (var child in ProgressChildren)
        {
            now += weightHandler(child.ProgressWeight) * child.Progress;
            total += child.ProgressWeight;
        }
        if (total == 0)
        {
            return 1f;
        }
        return now / total;
    }
    public delegate float GetResultOfProgressAndWeight(float progress, float weight);
    float GetProgressOfChildrenWithHandler(GetResultOfProgressAndWeight handler)
    {
        float now = 0, total = 0;
        foreach (var child in ProgressChildren)
        {
            now += handler(child.Progress, child.ProgressWeight);
            total += child.ProgressWeight;
        }
        if (total == 0)
        {
            return 1f;
        }
        return now / total;
    }
}
