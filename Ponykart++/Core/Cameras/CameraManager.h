#ifndef CAMERAMANAGER_H_INCLUDED
#define CAMERAMANAGER_H_INCLUDED

#include <vector>
#include "Kernel/LKernelObject.h"
#include "Core/Cameras/LCamera.h"


namespace Ponykart
{
namespace Core
{
/// Manages all of our cameras and handles switching between them as necessary.
class CameraManager : public LKernel::LKernelObject
{
public:
	CameraManager();
	// getters
	const LCamera* const getCurrentCamera(); ///< Gets the current camera that is being used for rendering.

public:
	std::vector<LCamera*> cameras;

private:
	LCamera* currentCamera;
};
} // Core
} // Ponykart

#endif // CAMERAMANAGER_H_INCLUDED
