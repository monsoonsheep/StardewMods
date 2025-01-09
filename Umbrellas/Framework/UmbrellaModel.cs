using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StardewMods.Umbrellas.Framework;
public class UmbrellaModel
{
    public string Texture = null!;

    public Vector2 Offset = Vector2.Zero;

    public List<Rectangle> Deletes = [];

    public List<SpriteEdit> Copies = [];
}
