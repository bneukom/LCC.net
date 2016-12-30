# LandscapeClassifier

Tool used to classify ortho images with several OpenCV classifiers.

# Features
- Import Sentinel-2 multispectral bands
- Import heightmodel bands
- Interactive feature training
- Prediction via SVM classifier (Accord.Net)
- Export of landcover type grayscale maps which can easily be imported into Unreal Engine

# How-to

TODO

# Tool
![Classification](http://i.imgur.com/DEkn9QG.jpg)

# Future Work
- Only WGS84 coordinate system for importing supported
- Better post processing support (color space adjustments)
- Shadow in the ortho images are very error prone, more classes (for example shadowed snow) or using an illumination invariant color space are needed as for example described by Maddern et al. in "Illumination Invariant Imaging: Applications in Robust Vision-based Localisation, Mapping and Classification for Autonomous Vehicles"
