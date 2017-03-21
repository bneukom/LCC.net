# LCC (LandCoverClassifier)

A tool used to classify multi spectral satellite images with several classifiers and the possibilties to assess accuracy and export the results. This tool was developed together with the [UnrealLandscape](https://github.com/bneukom/UnrealLandscape) which is used to visualize landscapes using the landcover maps generated from this tool.

To classify the data we use a simple workflow: first download the necessary data then add features for the supervised learning algorithm and finally train the algorithm and classify the whole image.

# Process
In this chapter the whole process from downloading the data to importing them into the Engine is described.
## Download and Import Data
The first step is downloading the data. For our purposes we used the Sentinel2 Satellite imagery which can be downloaded [here](https://scihub.copernicus.eu/dhus/#/home). Sentinel2 produces multispectral bands from the whole earth which are free to use. Using multispectral bands (not only the visible spectrum) increases the classification accuracy drastically [1]. Optionally the Sentinel2 can be preprocessed to remove artifacts from clouds or atmoshperic gasses as well as shadowing effects from the landscape using [Sen2Cor](http://step.esa.int/main/third-party-plugins-2/sen2cor/) (note that this requires a Python Anaconda installation).

Next we also use a heightmap as a feature for the classifaction process. This has been shown to increase classifcation results especially for mountainous regions [2]. The heightmap can also be exported during a later step used for the visualization. The data we used here is the Global Digital Elevation Model (GDEM) which can be downloaded [here](https://gdex.cr.usgs.gov/gdex/).

After downloading you should have several Sentinel2 bands (for example S2A_OPER_MSI_L1C_TL_SGS_20160823T173537_A006111_T32TLS_B\*) in JP2 formats as well as well as GeoTIFF heightmap. Theses layers can be added using the "Add Layer" button dialog which should look like this:

![Add Layer](http://i.imgur.com/ubxfuBx.png)

Contrast enhancement is used only for the Sentinel2 bands as they might contain very high intensities.

## Add Features
The next step is adding features. The default landcover types are designed for a mountainous region in Switzerland but can be changed using the "Change Landcovertypes" button. To train a supervised machine learning algorithm we need about 250 features per class. These features can be added by right clicking into the images at appropriate places using the appropriate landcover type. The features used for the training consist of the previously added Sentinel2 bands as well as the heightmap.

# Future Work
- Only WGS84 coordinate system for importing supported
- Better post processing support (color space adjustments)
