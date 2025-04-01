---
title: Texture Patterns
description: Examples of how to use texture patterns to make color, normal, and ORM maps.
---

When creating a color map, normal map, or ORM map you can use the built-in pattern generators to make interesting textures.

??? example "Continuing "Hello Cube""
	In the "Hello Cube" example we created a simple color map with a maroon colour:
	
	`#!csharp using var colorMap = materialBuilder.CreateColorMap(StandardColor.Maroon);`

	You can try out the examples below by simply replacing the `colorMap` with some more interesting patterns!

	If you're feeling adventurous you can also experiment with adding a `normalMap` and even an `ormMap` to the cube's material too.

??? question "What are Color, Normal, and ORM Maps?"
	A 'map' is ultimately just 2D texture/bitmap, and all three map types work together to form a single material:
	
	:material-palette: A __color__ map is the texture that provides the surface colors (also known as 'albedo') for the material.

	:material-axis-arrow: A __normal__ map is the texture that affects how lighting bounces off the material surface by defining which direction each pixel "faces" relative to the flat surface. Normal maps can be used to give the illusion of surface 'texture' or detail without actually having to modify the polygon mesh.

	:material-texture: An __ORM__ map is actually three values baked in to one texture, and is used to add additional information about the nature of the material surface:

	* :material-alpha-o-box-outline: The __Occlusion__ channel is stored in the Red pixel data of the texture and defines on a scale of 0.0 to 1.0 how much light is blocked (or 'occluded') from reaching each pixel on the material surface. For example, if you're trying to simulate a surface of wooden boards, it's likely that less light will reach in the crevices between the boards than on the surface of the boards themselves.

	* :material-alpha-r-box-outline: The __Roughness__ channel is stored in the Green pixel data of the texture and defines on a scale of 0.0 to 1.0 how 'rough' the material is at each pixel of its surface. This value is used to determine how 'shiny' the light reflections are on each part of the surface.
	
	* :material-alpha-m-box-outline: The __Metallic__ channel is stored in the Blue pixel data of the texture and defines on a scale of 0.0 to 1.0 how 'metallic' the material is at each pixel of its surface. In reality, most materials' surfaces' texel data should always be 0.0 (non-metallic) or 1.0 (metallic).

	For more information, see: [:octicons-arrow-right-24: Materials](/concepts/materials.md)

All texture patterns can be created by using the static methods on the `TexturePattern` class, located in the `Egodystonic.TinyFFR.Assets.Materials` namespace.

Any type of map can be created with any pattern type, it just depends what type of value you specify for the pattern's "value" arguments:

* For color maps, you should make a texture pattern of `ColorVect`s.
* For normal maps, you should make a texture pattern of `Direction`s.
* For occlusion, roughness, or metallic maps, you should make a texture pattern of `Real`s.

The following examples will show you how to create texture patterns:

## Chequerboard Color Maps

=== "Bordered, 2 Colours"

	![Cube with chequerboard color map applied](texture_patterns_chequerboard.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Chequerboard texture pattern
	///

	For this first example, we will create a color map using a `ChequerboardBordered` texture pattern:

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(
		TexturePattern.ChequerboardBordered(
			borderValue: ColorVect.FromRgb24(0x880000), // (1)!
			borderWidth: 8, // (2)!
			firstValue: ColorVect.White, // (3)!
			secondValue: ColorVect.Black, // (4)!
			repetitionCount: (8, 8), // (5)!
			cellResolution: 120 // (6)!
		)
	);
	```

	1. 	This line is setting the colour of the chequerboard borders.

		`ColorVect.FromRgb24()` allows you to specify colours as hex codes. You can also create a `ColorVect` from hue/saturation/lightness using `ColorVect.FromHueSaturationLightness()`, or specify the RGB components directly by using the constructor (i.e. `new ColorVect(r, g, b)`).

	2.	This line sets the width of the border around each cell (square), in pixels.

	3. 	This is setting the colour of the first cell (square) and every even-numbered cell after that.

	4. 	This is setting the colour of the second cell (square) and every odd-numbered cell after that.

	5.	This is setting the number of repetitions (i.e. the grid size of the texture). We want an 8x8 board so we specify repetition count as `(8, 8)`. 

		Note: In actuality, the type of the expression `(8, 8)` is [ValueTuple&lt;int, int&gt;](https://learn.microsoft.com/en-us/dotnet/api/system.valuetuple-2?view=net-9.0). The tuple is being implicitly converted to an `XYPair<int>`, which a TinyFFR type that `repetitionCount` is declared as.

	6.	This is setting the size, in pixels, of the width and depth of each cell (square).

=== "Bordered, 4 Colours"

	There are some overloads of `ChequerboardBordered` that can take a `thirdValue` and/or `fourthValue` too if you prefer. Here's another example using four colours and an uneven repetition count:

	![Image of chequerboard cube with random colours](texture_patterns_chequerboard_random.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Four colours picked at random, uneven repetition count
	///

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(
		TexturePattern.ChequerboardBordered(
			borderValue: ColorVect.RandomOpaque(),
			borderWidth: 16,
			firstValue: ColorVect.RandomOpaque(),
			secondValue: ColorVect.RandomOpaque(),
			thirdValue: ColorVect.RandomOpaque(),
			fourthValue: ColorVect.RandomOpaque(),
			repetitionCount: (10, 6),
			cellResolution: 200
		)
	);
	```

=== "Unbordered"

	There is also a variant pattern called `Chequerboard` (instead of `ChequerboardBordered`) that does not include a border:

	![Cube with non-bordered chequerboard pattern](texture_patterns_chequerboard_borderless.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Red / yellow / green / blue, no border
	///

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(TexturePattern.Chequerboard(
		firstValue: ColorVect.FromStandardColor(StandardColor.Red), // (1)!
		secondValue: ColorVect.FromStandardColor(StandardColor.Green),
		thirdValue: ColorVect.FromStandardColor(StandardColor.Blue),
		fourthValue: ColorVect.FromStandardColor(StandardColor.Yellow)
	));
	```

	1. 	`ColorVect.FromStandardColor()` can also be replaced with just an implicit conversion from `StandardColor`, e.g. you can write this line simply as:

		`#!csharp firstValue: StandardColor.Red,`

## Circle or Rectangle Color Maps

=== "3x3 Circles"

	![Cube with bordered circles texture](texture_patterns_circles_simple.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Nine bordered circles
	///

	In this example, we create a 3x3 'grid' of bordered circles. We specify each colour in [HSL](https://en.wikipedia.org/wiki/HSL_and_HSV) format with the static method `ColorVect.FromHueSaturationLightness()`. The first argument to `FromHueSaturationLightness()` is a hue angle in degrees, the second is a saturation (from 0.0 to 1.0), and the third is a lightness (also from 0.0 to 1.0):

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(TexturePattern.Circles(
		interiorValue: ColorVect.FromHueSaturationLightness(180f, 0.6f, 0.33f), // (1)!
		borderValue: ColorVect.FromHueSaturationLightness(-70f, 1f, 0.5f), // (2)!
		paddingValue: ColorVect.FromHueSaturationLightness(240f, 0.3f, 0.7f), // (3)!
		repetitions: (3, 3) // (4)!
	));
	```

	1. This is the colour of the interior of each circle.
	2. This is the colour of the border of each circle.
	3. This is the colour between the circles.
	4. Just like with the chequerboard patterns, this specifies the number of circles in each direction.

=== "Interpolated Circle"

	![Circle with interpolated colouring](texture_patterns_circle_interpolated.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	A single bordered circle with interpolated colouring
	///

	Some of the overloads for `TexturePattern.Circle()` work with interpolatable values (`ColorVect` is interpolatable). In the following example, we will set colour values for the top, left, right, and bottom of the border and interior of a circle, and the texture pattern will interpolate values around the circle between those four "stops".

	This example also uses some slightly more complicated constructions for `ColorVect`s:
	
	1. As we saw in the previous example, we can specify colours in HSL format. The first argument to `ColorVect.FromHueSaturationLightness()` is the hue angle.
	2. To make our interpolated colour wheel look nice, we set the right, top, left and bottom hue angles by converting them from corresponding `Orientation2D` values. `Orientation2D` is an enum that represents some base axes in 2D, and we can convert an `Orientation2D` to an `Angle` with the method `ToPolarAngle()`.
	3. `ToPolarAngle()` can return `null` if we invoke it on `Orientation2D.None`, but as we know we are not trying to convert a `None` orientation to an angle, we can use the [null-forgiving operator](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/null-forgiving) and assume there's a `Value`.

	```csharp
	var rightAngle = Orientation2D.Right.ToPolarAngle()!.Value;
	var topAngle = Orientation2D.Up.ToPolarAngle()!.Value;
	var leftAngle = Orientation2D.Left.ToPolarAngle()!.Value;
	var bottomAngle = Orientation2D.Down.ToPolarAngle()!.Value;

	using var colorMap = materialBuilder.CreateColorMap(TexturePattern.Circles(
		interiorValueRight: ColorVect.FromHueSaturationLightness(rightAngle, 1f, 0.3f),
		interiorValueTop: ColorVect.FromHueSaturationLightness(topAngle, 1f, 0.3f),
		interiorValueLeft: ColorVect.FromHueSaturationLightness(leftAngle, 1f, 0.3f),
		interiorValueBottom: ColorVect.FromHueSaturationLightness(bottomAngle, 1f, 0.3f),

		borderValueRight: ColorVect.FromHueSaturationLightness(rightAngle + 90f, 1f, 0.5f), // (1)!
		borderValueTop: ColorVect.FromHueSaturationLightness(topAngle + 90f, 1f, 0.5f),
		borderValueLeft: ColorVect.FromHueSaturationLightness(leftAngle + 90f, 1f, 0.5f),
		borderValueBottom: ColorVect.FromHueSaturationLightness(bottomAngle + 90f, 1f, 0.5f),

		paddingValue: ColorVect.White.WithLightness(0.2f), // (2)!

		repetitions: (1, 1)
	));
	```

	1. Notice that we're shifting the hue colour angle for each border stop by 90°, mostly to help it stand out from the interior colour wheel.
	2. `WithLightness()` returns a new `ColorVect` with the HSL lightness adjusted to the given value (in this case we're returning `White` with a lightness of `0.2`).

=== "Simple Rectangles"

	![Cube with an array of rectangles displayed](texture_patterns_simple_rectangles.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	A very simple repetition of red rectangles on a green background
	///

	This example shows how to generate a rectangles pattern using only two arguments. If desired, it's also possible to specify a `borderValue`, but this is optional:

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(
		TexturePattern.Rectangles(
			interiorValue: new ColorVect(1f, 0f, 0f),
			paddingValue: new ColorVect(0f, 1f, 0f)
		)
	);
	```

=== "Bordered Squares"

	![Cube with squares bordered with different colours](texture_patterns_bordered_squares.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Four squares each with multi-coloured borders
	///

	Not only can you specify a border for each "rectangle", but you can actually specify a different value for the top, left, bottom and right sides (optionally):

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(
		TexturePattern.Rectangles(
			interiorSize: (64, 64),
			borderSize: (8, 8),
			paddingSize: (32, 32),
			interiorValue: new ColorVect(1f, 1f, 1f),
			borderRightValue: new ColorVect(1f, 1f, 0f),
			borderTopValue: new ColorVect(1f, 0f, 0f),
			borderLeftValue: new ColorVect(0f, 1f, 0f),
			borderBottomValue: new ColorVect(0f, 0f, 1f),
			paddingValue: new ColorVect(0f, 0f, 0f),
			repetitions: (2, 2)
		)
	);
	```

## Circle or Rectangle Normal Maps

=== "Rectangular Normal Pattern"

	![Cube with squares bordered with different colours](texture_patterns_bordered_squares.png){ style="height:200px;width:200px;border-radius:12px"}
	/// caption
	Four squares each with multi-coloured borders
	///

	Normal maps in TinyFFR are specified as textures of `Direction`s. Just like with `ColorVect`, we can use the texture pattern generator to create `Direction` patterns in the exact same way.

	??? question "What does the 'Direction' of a pixel mean?"
		agadgad

	By [convention](/concepts/conventions.md), `Direction.Forward` is the "default" direction for a surface

	```csharp
	using var colorMap = materialBuilder.CreateColorMap(
		TexturePattern.Rectangles(
			interiorSize: (64, 64),
			borderSize: (8, 8),
			paddingSize: (32, 32),
			interiorValue: new ColorVect(1f, 1f, 1f),
			borderRightValue: new ColorVect(1f, 1f, 0f),
			borderTopValue: new ColorVect(1f, 0f, 0f),
			borderLeftValue: new ColorVect(0f, 1f, 0f),
			borderBottomValue: new ColorVect(0f, 0f, 1f),
			paddingValue: new ColorVect(0f, 0f, 0f),
			repetitions: (2, 2)
		)
	);
	```

## Line ORM Maps

## Gradients

## Plain Fills

## Transforms
