# Personalized Rendering Variant

## Idea 

I see rendering variant as different component compare to traditional Sitecore renderings. Currently Sitecore supports personalization on Renderings directly but not Rendering Variants. You can still achieve the personalization with in rendering variant, but not between the variant.

## Solution

Currently, a content author has no way to assign the rendering variant to the SXA component when personalization rule matches. Thus, the purpose of this module is to allow the user to assign the rendering variants to the SXA Component as part of personalization setup.

Once personalization in place, then when rule condition matches, it will apply the respective rendering variant to the rendering component.


## Doesn't Support:

Experience Editor: A user cannot see the variant applied when they change the personalization from the dropdown list during edit mode.

## Suggestion/Improvements

Developers are welcome to provided the suggestion or contirubute their improvements to this repository.

## Technology

This module is developed on Sitecore 9.1 and SXA 8.1
