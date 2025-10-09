# Process Journal

## 10.01.25 | Why?

This project is a quick turn-around idea for the [2025 Whaaat!? Festival](https://whaaat.io) that is situated in the [playthings](https://github.com/mouseandthebillionaire/planetVelocityMap/blob/main/Proposal/README.md#playthings) section of the thesis work. The core goal here is to explore how the physical interfaces of games can be objects of and vehicles for contemplation. I am thinking specifically of objects of worship and/or ceremonial objects (prayer candles, stained-glass, mandalas, icons[^1], idols, etc) and how they point out to something beyond them, but while this concept can be easily understood from a spiritual context, I think there is also something to be explored about the use of objects for non-spiritual contemplative ends (Japanese tea ceremony being a clear precedent)

Within play then, can objects create a state of contemplation in the player? It's worth being reminded that the working definition for contemplation is the one taken from Djebbara et al where it is a "heightened level of awareness and an intentional focus on the present moment, fostering a state of deep reflection and non-judgmental engagement." This seems fairly reasonable, right? Are players being 'contemplative' when they are intensely focused on their games? I think maybe (?), but for this project I would like to focus on the transference of focus from the object to the external world. 

So, some design goals:
- A physical interface that does not read "game interface"
- Ambiguous visuals and sounds. I don't want the player to feel as they have 'solved' anything
- Extended time - instead it's more about how long they have spent with the object, learning from the micro-movements

## 10.05.25 | Ideation

![Initial Ideation](Media/ideation.jpg)

This image in the top left corner is getting closest to what I am envisioning for this project. I like some of these other ideas as well, but I think the hidden force-sensor grid on a board (with some sort of thick overlay to dissipate the force) allows for some serious ambiguity and therefore time spent with the thing. Additionally, the idea of it being on a board reads a little [prie-dieu](https://churchantiques.com/product/excellent-quality-1910-carved-gothic-oak-small-prie-dieu-from-st-saviours-sunbury-sale/) which I like. 

I think eventually, I would really like this on top of some sort of box, or behind a display area of some sort so that there is a third article that becomes the exact _object_ of contemplation. The experience could change based on what is placed there. Essentially some sort of contemplative [amiibo](https://www.nintendo.com/us/amiibo/) situation.[^2]

First steps:
- Knock up a version that takes in keyboard inputs. I can switch this over to the controller once I build it in Colorado
- Order the components and ship them to Boulder
- Think about the sound/visuals. I have no idea what these will be, and this could easily veer off into screen-saver territory, so I'll have to be a little careful there

## 10.08.25 | Unity Pass

I realized as soon as I started knocking this together that I was going to need the force sensors here to really test this thing[^3]. I also realized when I pulled out the Arduino that it only has six analog inputs, so the sketch I had with twelve isn't going to fly. Theoretically, obviously I could use two arduinos (or a different microcontroller entirely) but I really want this thing to function more as a proof-of-concept, and there's no real need to get too complicated. All that to say, I shipped the FSRs here so that I'll have them by next week to mock some things up.

In the mean-time, I built the back-end of this thing, and have a keyboard setup for testing. It doesn't recognize the force, obviously, but there's a slider to sort of mimic that. The functionality is pretty simple. When you hold down the key (eventually FSR) it starts counting up, and that speed is increased as the force goes up. Should give us a pretty good little number to play with.

I tried getting Claude to quickly spin up a version of [this](https://github.com/Bleuje/interactive-physarum?tab=readme-ov-file), but it was a bit of a disaster. I love the idea, but might be a bit too complicated for this thing anyway. Though maybe for a future iteration. For this sprint I decided to pivot and use the same technique that I used for [Instauratio Exiguus](https://github.com/mouseandthebillionaire/losFinisCDE) which was rely easy to set up, since I've used it a few times. One change I made though, is to use a video instead of text. I've always liked [this video](https://vimeo.com/406428324) that L made, so I grabbed the first section for this. I exported out 36 frames, and then split those into six layers. Then six different sprites just cycle through those frames. Easy peasy! It's a lot of sprites, but hopefully won't be an issue. Now we have control of all those layers to rotate and apply effects (which will be the next step)

![First Visual Pass](Media/layerSplit.gif)

There's an immediate worry that the visuals may be a bit _too_ subtle, but I'll start playing with it and maybe do some treatment to the sprites if I need. It also points towards the question of whether or not the _whole thing_ might be too subtle, but it's a delicate balance when you're not trying to hit people over the head with a thing. The textual elements worked really well with IE, so I might go back to that just to have _something_ for people to be pointed to. We'll see.


---
## Notes

[^1]: Hence the repo name...
[^2]: Which, not gonna lie, not a terrible idea, Haha
[^3]: If I've learned one thing from teaching, it's don't count on things to work out down the road...