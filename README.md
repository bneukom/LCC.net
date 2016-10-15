# LandscapeClassifier

Tool used to classify ortho images with several OpenCV classifiers.

## Features
- Import ortho images and DEMs
- Interactive feature training
- Prediction via Bayes- KNN- NeuralNetwork-classifiers (OpenCV)
- Export of landcover type binary maps which can easily be imported into Unreal Engine


## Sample images

### Classification
![Classification](http://i.imgur.com/piTn1pA.jpg)

### Prediction
![Prediction](http://i.imgur.com/IUZrfxX.jpg)

## TODO

- As of now only ortho images which have the same projection (for example CH1903) and same resolution (for example 25m/pixel) 
can be imported correctly. Transformations between different types needs to be implemented.
- Shadow in the ortho images are very error prone, more classes (for example shadowed snow) or using an illumination invariant color space are needed as for example described by Maddern et al. in "Illumination Invariant Imaging: Applications in Robust Vision-based
Localisation, Mapping and Classification for Autonomous Vehicles"
- Neural Network needs better calibration
