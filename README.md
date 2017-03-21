# LCC (Land Cover Classifier)

A tool used to classify multi spectral satellite images with several classifiers and the possibilties to assess accuracy and export the results. This tool was developed together with the [UnrealLandscape](https://github.com/bneukom/UnrealLandscape) which is used to visualize landscapes using the landcover maps generated from this tool.

To classify the data we use a simple workflow: first download the necessary data then add features for the supervised learning algorithm and finally train the algorithm and classify the whole image.

# Process
The following sections describe the whole process from downloading the data to exporting them for further use.

## Download and Import Data
The first step is downloading the data. For our purposes we used the Sentinel2 Satellite imagery which can be downloaded [here](https://scihub.copernicus.eu/dhus/#/home). Sentinel2 produces multispectral imagery from the whole earth which are free to use. Using multispectral bands (not only the visible spectrum) increases the classification accuracy drastically [1]. Optionally the Sentinel2 can be postprocessed to remove artifacts from clouds or atmoshperic gasses as well as shadowing effects from the landscape using [Sen2Cor](http://step.esa.int/main/third-party-plugins-2/sen2cor/) (note that this requires a Python Anaconda installation).

We also use a heightmap as a feature for the classifaction process. This has been shown to increase classifcation results especially for mountainous regions [2]. The heightmap can also be exported during a later step used for the visualization. The data we used here is the Global Digital Elevation Model (GDEM) which can be downloaded [here](https://gdex.cr.usgs.gov/gdex/).

After downloading you should have several Sentinel2 bands (for example S2A_OPER_MSI_L1C_TL_SGS_20160823T173537_A006111_T32TLS_B\*) in JP2 format as well as well as GeoTIFF heightmap. Theses layers can be added using the "Add Layer" button dialog which should look like this:

<p align="center">
   <img src="http://i.imgur.com/ubxfuBx.png" alt="Add Layer" width="600"/>
</p>


Contrast enhancement is used for the Sentinel2 bands as they might contain very high intensities.

## Add Features
The next step is adding features. The default landcover types are designed for a mountainous region in Switzerland but can be changed using the "Change Landcovertypes" button. To train a supervised machine learning algorithm we need about 250 features per class. These features can be added by right clicking into the image region at appropriate places using the appropriate landcover type. The feature vectors used for the machine learning algorithm consist of the previously added Sentinel2 bands as well as the heightmap. The features can be seen on the righthand side which should now look something like this:

![Features](http://i.imgur.com/nayFTu0.jpg)

The fetures can also be saved to the disk using the "Export Features" button and later be loaded again using the "Import Features" button.

## Training and Accuracy Assessement
Our results as well as the litarture shows that a SVM achieves very high performance as well as appropriate performance [3]. The Gaussian Kernel achieves the best result but we recommend using the linear Kernel simply due to the better performance. The Complexity paramater of the SVM can be estimated using a simple grid search or by trial and error. The classifier can be trained using the "Train" button. The accuracy can be evaluated using the "Compute Confusion Matrix" button which uses cross validation to evaluate the accuracy of the classifier. The results should look something like this:

![Accuracy](http://i.imgur.com/ykkyFXL.png)

An accuracy of over 90% is in agreement with current studies using SVMs and is considered very good. We can now use this classifier to generate a landcover map for the whole region.

## Prediction
The whole region can be predicted using the "Predict All" button (which might take some time). The prediction is the rendered over the bands with a transparency set using the "Prediction Transparency" slider. Using a pixel based classifier might result in noise (single pixel missclassifications) which can be reduced using a simple majority filter using the "Apply Majority Filter" button. This results in more contiguous landcover regions. The prediction should look something like this:

![Prediction](http://i.imgur.com/LmP6kgy.jpg)

## Export
After the prediction we can now export the landcover types. If used for a visualization using [UnrealLandscape](https://github.com/bneukom/UnrealLandscape) the "Crop/Scale Output to Unreal Landscape Dimension" should be checked to crop the result to a dimension which leaves no borders when imported to the Unreal Engine. In the first list the bands previously loaded can be exported as well. For a visualization we need the RGB band (exported as PBGRA) and the heightmap band (exported as Grayscale16).

# Disclaimer
Be aware that the tool is nowhere near complete and lacks several important features and might contain bugs.

# Future Work
* Improve grid search with custom ranges
* Support for more image datatypes and refactor image loading
* Support for full reprojection of the input data, right now we only support scaling

# References
