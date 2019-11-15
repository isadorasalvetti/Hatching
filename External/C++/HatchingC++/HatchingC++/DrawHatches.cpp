#include "DrawHatches.h"

cv::Mat HatchedImage;
int _thickness = 3;

void DrawHatchLines(std::vector<std::vector<cv::Point>> lines) {
	for (unsigned int i = 0; i < lines.size; i++) {
		cv::InputArrayOfArrays points = cv::InputArrayOfArrays(lines[i]);
		for (unsigned int j = 0; j < lines.size; j++) {
			cv::polylines(HatchedImage, points, false, cv::Scalar(0, 0, 0, 1), _thickness);
		}
	}
}