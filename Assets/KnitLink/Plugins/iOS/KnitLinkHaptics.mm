// Knit Link – iOS haptics (27/10). Called from Game/Haptics.cs (DllImport "__Internal").
// style: 0 = light, 1 = medium, 2 = heavy  (UIImpactFeedbackGenerator, the Taptic Engine)
#import <UIKit/UIKit.h>

extern "C" void KLHapticImpact(int style)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UIImpactFeedbackStyle s = style == 0 ? UIImpactFeedbackStyleLight : style == 1 ? UIImpactFeedbackStyleMedium : UIImpactFeedbackStyleHeavy;
        UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
        [g prepare];
        [g impactOccurred];
    });
}
