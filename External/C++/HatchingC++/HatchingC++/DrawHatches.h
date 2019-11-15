#ifndef DRAW_HATCHES // include guard
#define DRAW_HATCHES

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>

using namespace std;

class DrawHatches
{
	DrawHatches();
	cv::Mat HatchedImage;

	private:
		void DrawHatchLines(std::vector<std::vector<cv::Point>> lines);
};

#endif
