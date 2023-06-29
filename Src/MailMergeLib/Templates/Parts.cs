using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MailMergeLib.Templates;

/// <summary>
/// The class is a container for a list of type <see cref="Part"/>.
/// </summary>
public class Parts : ObservableCollection<Part>
{
    /// <summary>
    /// Adds an object to the end of the list.
    /// </summary>
    /// <param name="newItem"></param>
    public new void Add(Part newItem)
    {
        ThrowIfPartAlreadyExists(newItem);
        base.Add(newItem); 
    }

    /// <summary>
    /// Adds the elements of the specified collection to the end of the list.
    /// </summary>
    /// <param name="newItems"></param>
    public void AddRange(IEnumerable<Part> newItems)
    {
        var ni = newItems.ToArray();
        foreach (var item in ni)
        {
            ThrowIfPartAlreadyExists(item);
            base.Add(item);
        }
    }

    private void ThrowIfPartAlreadyExists(Part newItem)
    {
        if (this.Any(part => part.Key == newItem.Key && part.Type == newItem.Type))
        {
            throw new TemplateException($"A part with key '{newItem.Key}' and type '{newItem.Type}' already exists in the list.", newItem, this, null, null);
        }
    }

    /// <summary>
    /// Compares the Parts with an other instance of Parts for equality.
    /// </summary>
    /// <param name="other"></param>
    /// <returns>Returns true, if both instances are equal, else false.</returns>
    private bool Equals(Parts other)
    {
        // not any entry missing in this, nor in the other list
        return !this.Except(other).Union(other.Except(this)).Any();
    }

    /// <summary>
    /// Compares this instance with an other instance of Parts for equality.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Returns true, if both instances are equal, else false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Parts) obj);
    }

    /// <summary>
    /// Determines the hash code of this instance.
    /// </summary>
    /// <returns>Returns the hash code of this instance.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            return this.Aggregate(0, (current, item) => (current * 397) ^ (item != null ? item.GetHashCode() : 0));
        }
    }
}
