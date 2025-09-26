// https://github.com/CedricGuillemet/ImGuizmo
// v1.91.3 WIP
//
// The MIT License(MIT)
//
// Copyright(c) 2021 Cedric Guillemet
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// https://github.com/HexaEngine/Hexa.NET.ImGui.Widgets/
//
// Copyright(c) 2025 Juna Meinhold
//
// This file includes modifications to the original MIT-licensed code.
// The original code copyright is retained above.
//
// Modified and ported by Juna, have fun!
// And be careful not to shoot yourself in the foot with it!
// :3
//

using Hexa.NET.ImGui.Widgets.Extensions;
using Hexa.NET.Utilities.Text;
using System.Numerics;

namespace Hexa.NET.ImGui.Widgets.ImSequencer
{
    public static unsafe class ImSequencer
    {
        private static bool SequencerAddDelButton(ImDrawList* draw_list, Vector2 pos, bool add = true)
        {
            var io = ImGui.GetIO();
            ImRect btnRect = new(pos, new Vector2(pos.X + 16, pos.Y + 16));

            bool overBtn = btnRect.Contains(io.MousePos);
            bool containedClick = overBtn && btnRect.Contains(io.MouseClickedPos[0]);
            bool clickedBtn = containedClick && io.MouseReleased[0];
            uint btnColor = overBtn ? 0xAAEAFFAA : 0x77A3B2AA;

            if (containedClick && io.MouseDownDuration[0] > 0)
            {
                btnRect.Expand(2.0f);
            }

            float midy = pos.Y + 16 / 2 - 0.5f;
            float midx = pos.X + 16 / 2 - 0.5f;
            draw_list->AddRect(btnRect.Min, btnRect.Max, btnColor, 4);
            draw_list->AddLine(new Vector2(btnRect.Min.X + 3, midy), new Vector2(btnRect.Max.X - 3, midy), btnColor, 2);
            if (add)
            {
                draw_list->AddLine(new Vector2(midx, btnRect.Min.Y + 3), new Vector2(midx, btnRect.Max.Y - 3), btnColor, 2);
            }

            return clickedBtn;
        }

        static ImSequencerContext seqCtx = new();

        public static bool Sequencer(SequenceInterface sequence, ref int currentFrame, ref bool expanded, ref int selectedEntry, ref int firstFrame, SequencerOptions sequenceOptions)
        {
            fixed (int* pCurrentFrame = &currentFrame)
            fixed (bool* pExpanded = &expanded)
            fixed (int* pSelectedEntry = &currentFrame)
            fixed (int* pFirstFrame = &currentFrame)
            {
                return Sequencer(sequence, pCurrentFrame, pExpanded, pSelectedEntry, pFirstFrame, sequenceOptions);
            }
        }

        public struct SequencerState
        {
            public ImDrawList* DrawList;
            public Vector2 CanvasPos;
            public Vector2 CanvasSize;
            public Vector2 ContentMin;
            public Vector2 ContentMax;
            public int FirstFrameUsed;
            public int LegendWidth = 200;
            public int ItemHeight = 20;
            public ImVector<ImSequencerCustomDraw> CustomDraws;
            public ImVector<ImSequencerCustomDraw> CompactCustomDraws;
            public int CurrentFrame;

            public SequencerState()
            {
            }

            public void Begin()
            {
                DrawList = ImGui.GetWindowDrawList();
                CanvasPos = ImGui.GetCursorScreenPos();
                CanvasSize = ImGui.GetContentRegionAvail();
                CustomDraws.Clear();
                CompactCustomDraws.Clear();
            }

            public void DrawLine(SequenceInterface sequence, int modFrameCount, int halfModFrameCount, int i, int regionHeight)
            {
                bool baseIndex = i % modFrameCount == 0 || i == sequence.GetFrameMax() || i == sequence.GetFrameMin();
                bool halfIndex = i % halfModFrameCount == 0;
                int px = (int)CanvasPos.X + (int)(i * seqCtx.FramePixelWidth) + LegendWidth - (int)(FirstFrameUsed * seqCtx.FramePixelWidth);
                int tiretStart = baseIndex ? 4 : (halfIndex ? 10 : 14);
                int tiretEnd = baseIndex ? regionHeight : ItemHeight;

                if (px <= CanvasSize.X + CanvasPos.X && px >= CanvasPos.X + LegendWidth)
                {
                    DrawList->AddLine(new Vector2(px, CanvasPos.Y + tiretStart), new Vector2(px, CanvasPos.Y + tiretEnd - 1), 0xFF606060, 1);

                    DrawList->AddLine(new Vector2(px, CanvasPos.Y + ItemHeight), new Vector2(px, CanvasPos.Y + regionHeight - 1), 0x30606060, 1);
                }

                if (baseIndex && px > CanvasPos.X + LegendWidth)
                {
                    byte* tmps = stackalloc byte[512];
                    Utf8Formatter.Format(i, tmps, 512);
                    DrawList->AddText(new Vector2(px + 3.0f, CanvasPos.Y), 0xFFBBBBBB, tmps);
                }
            }

            public void DrawLineContent(int i, int regionHeight)
            {
                int px = (int)CanvasPos.X + (int)(i * seqCtx.FramePixelWidth) + LegendWidth - (int)(FirstFrameUsed * seqCtx.FramePixelWidth);
                int tiretStart = (int)ContentMin.Y;
                int tiretEnd = (int)ContentMax.Y;

                if (px <= CanvasSize.X + CanvasPos.X && px >= CanvasPos.X + LegendWidth)
                {
                    //draw_list->AddLine(Vector2((float)px, canvas_pos.Y + (float)tiretStart), Vector2((float)px, canvas_pos.Y + (float)tiretEnd - 1), 0xFF606060, 1);

                    DrawList->AddLine(new Vector2(px, tiretStart), new Vector2(px, tiretEnd), 0x30606060, 1);
                }
            }
        }

        static SequencerState state = new();

        public static bool Sequencer(SequenceInterface sequence, int* currentFrame, bool* expanded, int* selectedEntry, int* firstFrame, SequencerOptions sequenceOptions)
        {
            bool ret = false;
            var io = ImGui.GetIO();
            int cx = (int)io.MousePos.X;
            int cy = (int)io.MousePos.Y;

            ref int legendWidth = ref state.LegendWidth;

            int delEntry = -1;
            int dupEntry = -1;
            ref int ItemHeight = ref state.ItemHeight;

            bool popupOpened = false;
            int sequenceCount = sequence.GetItemCount();
            if (sequenceCount == 0)
            {
                return false;
            }

            state.Begin();

            ImDrawList* drawList = ImGui.GetWindowDrawList();
            ref Vector2 canvasPos = ref state.CanvasPos;
            ref Vector2 canvasSize = ref state.CanvasSize;

            ImRect bb = new(canvasPos, canvasSize);
            ImGuiP.ItemSize(bb);

            if (!ImGuiP.ItemAdd(bb, 1314, ref bb))
            {
                return false;
            }

            int firstFrameUsed = firstFrame != null ? *firstFrame : 0;
            state.FirstFrameUsed = firstFrameUsed;

            int controlHeight = sequenceCount * ItemHeight;
            for (int i = 0; i < sequenceCount; i++)
            {
                controlHeight += (int)sequence.GetCustomHeight(i);
            }

            int frameCount = Math.Max(sequence.GetFrameMax() - sequence.GetFrameMin(), 1);

            ref ImVector<ImSequencerCustomDraw> customDraws = ref state.CustomDraws;
            ref ImVector<ImSequencerCustomDraw> compactCustomDraws = ref state.CompactCustomDraws;

            // zoom in/out
            int visibleFrameCount = (int)MathF.Floor((canvasSize.X - legendWidth) / seqCtx.FramePixelWidth);
            float barWidthRatio = MathF.Min(visibleFrameCount / (float)frameCount, 1.0f);
            float barWidthInPixels = barWidthRatio * (canvasSize.X - legendWidth);

            ImRect regionRect = new(canvasPos, canvasPos + canvasSize);

            if (ImGui.IsWindowFocused() && io.KeyCtrl)
            {
                if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
                {
                    if (!seqCtx.PanningView)
                    {
                        seqCtx.PanningViewSource = io.MousePos;
                        seqCtx.PanningView = true;
                        seqCtx.PanningViewFrame = *firstFrame;
                    }
                    *firstFrame = seqCtx.PanningViewFrame - (int)((io.MousePos.X - seqCtx.PanningViewSource.X) / seqCtx.FramePixelWidth);
                    *firstFrame = ImMath.Clamp(*firstFrame, sequence.GetFrameMin(), sequence.GetFrameMax() - visibleFrameCount);
                }

                if (io.MouseWheel != 0.0f)
                {
                    float zoomSpeed = 1.1f;
                    float zoomSpeedInv = 1.0f / zoomSpeed;

                    float mouseX = io.MousePos.X - canvasPos.X - legendWidth;

                    int frameUnderCursor = *firstFrame + (int)(mouseX / seqCtx.FramePixelWidth);

                    if (io.MouseWheel > 0)
                        seqCtx.FramePixelWidthTarget *= zoomSpeed;
                    else if (io.MouseWheel < 0)
                        seqCtx.FramePixelWidthTarget *= zoomSpeedInv;

                    *firstFrame = frameUnderCursor - (int)(mouseX / seqCtx.FramePixelWidthTarget);
                    *firstFrame = ImMath.Clamp(*firstFrame, sequence.GetFrameMin(), sequence.GetFrameMax() - visibleFrameCount);
                }
            }

            if (seqCtx.PanningView && !io.MouseDown[0])
            {
                seqCtx.PanningView = false;
            }

            seqCtx.FramePixelWidthTarget = ImMath.Clamp(seqCtx.FramePixelWidthTarget, 0.1f, 50.0f);
            seqCtx.FramePixelWidth = ImMath.Lerp(seqCtx.FramePixelWidth, seqCtx.FramePixelWidthTarget, 0.33f);

            frameCount = sequence.GetFrameMax() - sequence.GetFrameMin();
            if (visibleFrameCount >= frameCount && firstFrame != null)
            {
                *firstFrame = sequence.GetFrameMin();
            }

            // --
            if (expanded != null && !*expanded)
            {
                ImGui.InvisibleButton("canvas"u8, new Vector2(canvasSize.X - canvasPos.X, ItemHeight));
                drawList->AddRectFilled(canvasPos, new Vector2(canvasSize.X + canvasPos.X, canvasPos.Y + ItemHeight), 0xFF3D3837);
                byte* tmps = stackalloc byte[512];
                StrBuilder builder = new(tmps, 512);
                sequence.FormatCollapse(ref builder, frameCount, sequenceCount);
                builder.End();
                drawList->AddText(new Vector2(canvasPos.X + 26, canvasPos.Y + 2), 0xFFFFFFFF, tmps);
            }
            else
            {
                bool hasScrollBar = true;

                Vector2 headerSize = new(canvasSize.X, ItemHeight);
                Vector2 scrollBarSize = new(canvasSize.X, 14.0f);

                Vector2 childFramePos = state.CanvasPos + new Vector2(0, headerSize.Y);
                Vector2 childFrameSize = new(canvasSize.X, canvasSize.Y - 8.0f - headerSize.Y - (hasScrollBar ? scrollBarSize.Y : 0));

                sequence.focused = ImGui.IsWindowFocused();
                //ImGui.InvisibleButton("contentBar"u8, new Vector2(canvasSize.X, controlHeight));
                Vector2 contentMin = state.ContentMin = childFramePos;
                Vector2 contentMax = state.ContentMax = childFramePos + new Vector2(0, controlHeight);
                ImRect contentRect = new(contentMin, contentMax);
                float contentHeight = contentMax.Y - contentMin.Y;

                // full background
                drawList->AddRectFilled(canvasPos, canvasPos + canvasSize, 0xFF242424);

                // current frame top
                ImRect topRect = new(new Vector2(canvasPos.X + legendWidth, canvasPos.Y), new Vector2(canvasPos.X + canvasSize.X, canvasPos.Y + ItemHeight));

                if (!seqCtx.MovingCurrentFrame && !seqCtx.PanningView && !seqCtx.MovingScrollBar && seqCtx.MovingEntry == -1 && (sequenceOptions & SequencerOptions.ChangeFrame) != 0 && currentFrame != null && *currentFrame >= 0 && topRect.Contains(io.MousePos) && io.MouseDown[0])
                {
                    seqCtx.MovingCurrentFrame = true;
                }
                if (seqCtx.MovingCurrentFrame)
                {
                    if (frameCount != 0)
                    {
                        var newFrame = (int)((io.MousePos.X - topRect.Min.X) / seqCtx.FramePixelWidth) + firstFrameUsed;
                        state.CurrentFrame = ImMath.Clamp(newFrame, sequence.GetFrameMin(), sequence.GetFrameMax());
                    }
                    if (!io.MouseDown[0])
                    {
                        seqCtx.MovingCurrentFrame = false;
                    }
                }

                //header
                drawList->AddRectFilled(canvasPos, new Vector2(canvasSize.X + canvasPos.X, canvasPos.Y + ItemHeight), 0xFF3D3837);
                if ((sequenceOptions & SequencerOptions.Add) != 0)
                {
                    if (SequencerAddDelButton(drawList, new Vector2(canvasPos.X + legendWidth - ItemHeight, canvasPos.Y + 2), true))
                    {
                        ImGui.OpenPopup("addEntry"u8);
                    }

                    if (ImGui.BeginPopup("addEntry"u8))
                    {
                        for (int i = 0; i < sequence.GetItemTypeCount(); i++)
                        {
                            if (ImGui.Selectable(sequence.GetItemTypeName(i)))
                            {
                                sequence.Add(i);
                                *selectedEntry = sequence.GetItemCount() - 1;
                            }
                        }

                        ImGui.EndPopup();
                        popupOpened = true;
                    }
                }

                //header frame number and lines
                const int baseModFrameCount = 10;
                const int baseFrameStep = 1;
                const float majorTickSpacing = 150;

                float ratio = majorTickSpacing / (baseModFrameCount * seqCtx.FramePixelWidth);
                int powerOf2 = Math.Max(1, (int)MathF.Ceiling(ImMath.Log2(ratio)));
                int modFrameCount = baseModFrameCount * (1 << powerOf2);
                int frameStep = baseFrameStep << powerOf2;
                int halfModFrameCount = modFrameCount / 2;

                for (int i = sequence.GetFrameMin(); i <= sequence.GetFrameMax(); i += frameStep)
                {
                    state.DrawLine(sequence, modFrameCount, halfModFrameCount, i, ItemHeight);
                }

                state.DrawLine(sequence, modFrameCount, halfModFrameCount, sequence.GetFrameMin(), ItemHeight);
                state.DrawLine(sequence, modFrameCount, halfModFrameCount, sequence.GetFrameMax(), ItemHeight);
                //draw_list->AddLine(canvas_pos, Vector2(canvas_pos.X, canvas_pos.Y + controlHeight), 0xFF000000, 1);
                //draw_list->AddLine(Vector2(canvas_pos.X, canvas_pos.Y + ItemHeight), Vector2(canvas_size.X, canvas_pos.Y + ItemHeight), 0xFF000000, 1);

                // clip content

                drawList->PushClipRect(childFramePos, childFramePos + childFrameSize, true);

                // draw item names in the legend rect on the left
                nuint customHeight = 0;
                for (int i = 0; i < sequenceCount; i++)
                {
                    int type;
                    sequence.Get(i, null, null, &type, null);
                    Vector2 tpos = new(contentMin.X + 3, contentMin.Y + i * ItemHeight + 2 + customHeight);
                    drawList->AddText(tpos, 0xFFFFFFFF, sequence.GetItemLabel(i));

                    if ((sequenceOptions & SequencerOptions.Del) != 0)
                    {
                        if (SequencerAddDelButton(drawList, new Vector2(contentMin.X + legendWidth - ItemHeight + 2 - 10, tpos.Y + 2), false))
                        {
                            delEntry = i;
                        }

                        if (SequencerAddDelButton(drawList, new Vector2(contentMin.X + legendWidth - ItemHeight - ItemHeight + 2 - 10, tpos.Y + 2), true))
                        {
                            dupEntry = i;
                        }
                    }
                    customHeight += sequence.GetCustomHeight(i);
                }

                // slots background
                customHeight = 0;
                for (int i = 0; i < sequenceCount; i++)
                {
                    uint col = (i & 1) != 0 ? 0xFF3A3636 : 0xFF413D3D;

                    nuint localCustomHeight = sequence.GetCustomHeight(i);
                    Vector2 pos = new(contentMin.X + legendWidth, contentMin.Y + ItemHeight * i + 1 + customHeight);
                    Vector2 sz = new(canvasSize.X + canvasPos.X, pos.Y + ItemHeight - 1 + localCustomHeight);
                    if (!popupOpened && cy >= pos.Y && cy < pos.Y + ((nuint)ItemHeight + localCustomHeight) && seqCtx.MovingEntry == -1 && cx > contentMin.X && cx < contentMin.X + canvasSize.X)
                    {
                        col += 0x80201008;
                        pos.X -= legendWidth;
                    }
                    drawList->AddRectFilled(pos, sz, col);
                    customHeight += localCustomHeight;
                }

                drawList->PushClipRect(childFramePos + new Vector2(legendWidth, 0.0f), childFramePos + childFrameSize, true);

                // vertical frame lines in content area
                for (int i = sequence.GetFrameMin(); i <= sequence.GetFrameMax(); i += frameStep)
                {
                    state.DrawLineContent(i, (int)contentHeight);
                }
                state.DrawLineContent(sequence.GetFrameMin(), (int)contentHeight);
                state.DrawLineContent(sequence.GetFrameMax(), (int)contentHeight);

                // selection
                bool selected = selectedEntry != null && *selectedEntry >= 0;
                if (selected)
                {
                    customHeight = 0;
                    for (int i = 0; i < *selectedEntry; i++)
                    {
                        customHeight += sequence.GetCustomHeight(i);
                    }

                    drawList->AddRectFilled(new Vector2(contentMin.X, contentMin.Y + ItemHeight * *selectedEntry + customHeight), new Vector2(contentMin.X + canvasSize.X, contentMin.Y + ItemHeight * (*selectedEntry + 1) + customHeight), 0x801080FF, 1.0f);
                }

                // slots
                customHeight = 0;

                ImRect* rects = stackalloc ImRect[3];
                uint* quadColor = stackalloc uint[3] { 0xFFFFFFFF, 0xFFFFFFFF, 0 };
                for (int i = 0; i < sequenceCount; i++)
                {
                    int* start, end;
                    uint color;
                    sequence.Get(i, &start, &end, null, &color);
                    nuint localCustomHeight = sequence.GetCustomHeight(i);

                    Vector2 pos = new(contentMin.X + legendWidth - firstFrameUsed * seqCtx.FramePixelWidth, contentMin.Y + ItemHeight * i + 1 + customHeight);
                    Vector2 slotP1 = new(pos.X + *start * seqCtx.FramePixelWidth, pos.Y + 2);
                    Vector2 slotP2 = new(pos.X + *end * seqCtx.FramePixelWidth + seqCtx.FramePixelWidth, pos.Y + ItemHeight - 2);
                    Vector2 slotP3 = new(pos.X + *end * seqCtx.FramePixelWidth + seqCtx.FramePixelWidth, pos.Y + ItemHeight - 2 + localCustomHeight);
                    uint slotColor = color | 0xFF000000;
                    uint slotColorHalf = (color & 0xFFFFFF) | 0x40000000;

                    if (slotP1.X <= canvasSize.X + contentMin.X && slotP2.X >= contentMin.X + legendWidth)
                    {
                        drawList->AddRectFilled(slotP1, slotP3, slotColorHalf, 2);
                        drawList->AddRectFilled(slotP1, slotP2, slotColor, 2);
                    }

                    if (new ImRect(slotP1, slotP2).Contains(io.MousePos) && io.MouseDoubleClicked[0])
                    {
                        sequence.DoubleClick(i);
                    }

                    // Ensure grabbable handles
                    float max_handle_width = slotP2.X - slotP1.X / 3.0f;
                    float min_handle_width = MathF.Min(10.0f, max_handle_width);
                    float handle_width = ImMath.Clamp(seqCtx.FramePixelWidth / 2.0f, min_handle_width, max_handle_width);
                    rects[0] = new ImRect(slotP1, new Vector2(slotP1.X + handle_width, slotP2.Y));
                    rects[1] = new ImRect(new Vector2(slotP2.X - handle_width, slotP1.Y), slotP2);
                    rects[2] = new ImRect(slotP1, slotP2);
                    quadColor[2] = slotColor + (selected ? 0u : 0x202020);

                    if (seqCtx.MovingEntry == -1 && (sequenceOptions & SequencerOptions.EditStartend) != 0)// TODOFOCUS && backgroundRect.Contains(io.MousePos))
                    {
                        for (int j = 2; j >= 0; j--)
                        {
                            ref ImRect rc = ref rects[j];
                            if (!rc.Contains(io.MousePos))
                            {
                                continue;
                            }

                            drawList->AddRectFilled(rc.Min, rc.Max, quadColor[j], 2);
                        }

                        for (int j = 0; j < 3; j++)
                        {
                            ref ImRect rc = ref rects[j];
                            if (!rc.Contains(io.MousePos))
                            {
                                continue;
                            }

                            if (!new ImRect(childFramePos, childFramePos + childFrameSize).Contains(io.MousePos))
                            {
                                continue;
                            }

                            if (ImGui.IsMouseClicked(0) && !seqCtx.MovingScrollBar && !seqCtx.MovingCurrentFrame)
                            {
                                seqCtx.MovingEntry = i;
                                seqCtx.MovingPos = cx;
                                seqCtx.MovingPart = j + 1;
                                sequence.BeginEdit(seqCtx.MovingEntry);
                                break;
                            }
                        }
                    }

                    // custom draw
                    if (localCustomHeight > 0)
                    {
                        Vector2 rp = new(canvasPos.X, contentMin.Y + ItemHeight * i + 1 + customHeight);
                        ImRect customRect = new(rp + new Vector2(legendWidth - (firstFrameUsed - sequence.GetFrameMin() - 0.5f) * seqCtx.FramePixelWidth, ItemHeight),
                           rp + new Vector2(legendWidth + (sequence.GetFrameMax() - firstFrameUsed - 0.5f + 2.0f) * seqCtx.FramePixelWidth, localCustomHeight + (nuint)ItemHeight));
                        ImRect clippingRect = new(rp + new Vector2(legendWidth, ItemHeight), rp + new Vector2(canvasSize.X, localCustomHeight + (nuint)ItemHeight));

                        ImRect legendRect = new(rp + new Vector2(0.0f, ItemHeight), rp + new Vector2(legendWidth, localCustomHeight));
                        ImRect legendClippingRect = new(canvasPos + new Vector2(0.0f, ItemHeight), canvasPos + new Vector2(legendWidth, localCustomHeight + (nuint)ItemHeight));
                        customDraws.PushBack(new ImSequencerCustomDraw(i, customRect, legendRect, clippingRect, legendClippingRect));
                    }
                    else
                    {
                        Vector2 rp = new(canvasPos.X, contentMin.Y + ItemHeight * i + customHeight);
                        ImRect customRect = new(rp + new Vector2(legendWidth - (firstFrameUsed - sequence.GetFrameMin() - 0.5f) * seqCtx.FramePixelWidth, (float)0.0f),
                           rp + new Vector2(legendWidth + (sequence.GetFrameMax() - firstFrameUsed - 0.5f + 2.0f) * seqCtx.FramePixelWidth, ItemHeight));
                        ImRect clippingRect = new(rp + new Vector2(legendWidth, (float)0.0f), rp + new Vector2(canvasSize.X, ItemHeight));

                        compactCustomDraws.PushBack(new ImSequencerCustomDraw(i, customRect, new ImRect(), clippingRect, new ImRect()));
                    }
                    customHeight += localCustomHeight;
                }

                // moving
                if (/*backgroundRect.Contains(io.MousePos) && */seqCtx.MovingEntry >= 0)
                {
                    ImGui.SetNextFrameWantCaptureMouse(true);

                    int diffFrame = (int)((cx - seqCtx.MovingPos) / seqCtx.FramePixelWidth);
                    if (Math.Abs(diffFrame) > 0)
                    {
                        int* start, end;
                        sequence.Get(seqCtx.MovingEntry, &start, &end, null, null);
                        if (selectedEntry != null)
                        {
                            *selectedEntry = seqCtx.MovingEntry;
                        }

                        ref int l = ref *start;
                        ref int r = ref *end;
                        if ((seqCtx.MovingPart & 1) != 0)
                        {
                            l += diffFrame;
                        }

                        if ((seqCtx.MovingPart & 2) != 0)
                        {
                            r += diffFrame;
                        }

                        if (l < 0)
                        {
                            if ((seqCtx.MovingPart & 2) != 0)
                            {
                                r -= l;
                            }

                            l = 0;
                        }
                        if ((seqCtx.MovingPart & 1) != 0 && l > r)
                        {
                            l = r;
                        }

                        if ((seqCtx.MovingPart & 2) != 0 && r < l)
                        {
                            r = l;
                        }

                        seqCtx.MovingPos += (int)(diffFrame * seqCtx.FramePixelWidth);
                    }
                    if (!io.MouseDown[0])
                    {
                        // single select
                        if (diffFrame == 0 && seqCtx.MovingPart != 0 && selectedEntry != null)
                        {
                            *selectedEntry = seqCtx.MovingEntry;
                            ret = true;
                        }

                        seqCtx.MovingEntry = -1;
                        sequence.EndEdit();
                    }
                }

                // cursor
                if (currentFrame != null && firstFrame != null && *currentFrame >= *firstFrame && *currentFrame <= sequence.GetFrameMax())
                {
                    float cursorOffset = contentMin.X + legendWidth + (*currentFrame - firstFrameUsed) * seqCtx.FramePixelWidth + seqCtx.FramePixelWidth / 2 - seqCtx.CursorWidth * 0.5f;
                    drawList->AddLine(new Vector2(cursorOffset, canvasPos.Y), new Vector2(cursorOffset, contentMax.Y), 0xA02A2AFF, seqCtx.CursorWidth);
                    byte* tmps = stackalloc byte[512];
                    Utf8Formatter.Format(*currentFrame, tmps, 512);
                    drawList->AddText(new Vector2(cursorOffset + 10, canvasPos.Y + 2), 0xFF2A2AFF, tmps);
                }

                drawList->PopClipRect();
                drawList->PopClipRect();

                for (int i = 0; i < customDraws.Size; ++i)
                {
                    ref var customDraw = ref customDraws.Data[i];
                    sequence.CustomDraw(customDraw.Index, drawList, ref customDraw.CustomRect, ref customDraw.LegendRect, ref customDraw.ClippingRect, ref customDraw.LegendClippingRect);
                }
                for (int i = 0; i < compactCustomDraws.Size; ++i)
                {
                    ref var customDraw = ref compactCustomDraws.Data[i];
                    sequence.CustomDrawCompact(customDraw.Index, drawList, ref customDraw.CustomRect, ref customDraw.ClippingRect);
                }

                // copy paste
                if ((sequenceOptions & SequencerOptions.Copypaste) != 0)
                {
                    ImRect rectCopy = new(new Vector2(contentMin.X + 100, canvasPos.Y + 2)
                       , new Vector2(contentMin.X + 100 + 30, canvasPos.Y + ItemHeight - 2));
                    bool inRectCopy = rectCopy.Contains(io.MousePos);
                    uint copyColor = inRectCopy ? 0xFF1080FF : 0xFF000000;
                    drawList->AddText(rectCopy.Min, copyColor, "Copy"u8);

                    ImRect rectPaste = new(new Vector2(contentMin.X + 140, canvasPos.Y + 2)
                       , new Vector2(contentMin.X + 140 + 30, canvasPos.Y + ItemHeight - 2));
                    bool inRectPaste = rectPaste.Contains(io.MousePos);
                    uint pasteColor = inRectPaste ? 0xFF1080FF : 0xFF000000;
                    drawList->AddText(rectPaste.Min, pasteColor, "Paste"u8);

                    if (inRectCopy && io.MouseReleased[0])
                    {
                        sequence.Copy();
                    }
                    if (inRectPaste && io.MouseReleased[0])
                    {
                        sequence.Paste();
                    }
                }
                //

                if (hasScrollBar)
                {
                    Vector2 scrollBarMin = childFramePos + new Vector2(0, childFrameSize.Y);
                    Vector2 scrollBarMax = scrollBarMin + scrollBarSize;

                    // ratio = number of frames visible in control / number to total frames

                    float startFrameOffset = (firstFrameUsed - sequence.GetFrameMin()) / (float)frameCount * (canvasSize.X - legendWidth);
                    Vector2 scrollBarA = new(scrollBarMin.X + legendWidth, scrollBarMin.Y - 2);
                    Vector2 scrollBarB = new(scrollBarMin.X + canvasSize.X, scrollBarMax.Y - 1);
                    drawList->AddRectFilled(scrollBarA, scrollBarB, 0xFF222222);

                    ImRect scrollBarRect = new(scrollBarA, scrollBarB);
                    bool inScrollBar = scrollBarRect.Contains(io.MousePos);

                    drawList->AddRectFilled(scrollBarA, scrollBarB, 0xFF101010, 8);

                    Vector2 scrollBarC = new(scrollBarMin.X + legendWidth + startFrameOffset, scrollBarMin.Y);
                    Vector2 scrollBarD = new(scrollBarMin.X + legendWidth + barWidthInPixels + startFrameOffset, scrollBarMax.Y - 2);
                    drawList->AddRectFilled(scrollBarC, scrollBarD, (inScrollBar || seqCtx.MovingScrollBar) ? 0xFF606060 : 0xFF505050, 6);

                    ImRect barHandleLeft = new(scrollBarC, new Vector2(scrollBarC.X + 14, scrollBarD.Y));
                    ImRect barHandleRight = new(new Vector2(scrollBarD.X - 14, scrollBarC.Y), scrollBarD);

                    bool onLeft = barHandleLeft.Contains(io.MousePos);
                    bool onRight = barHandleRight.Contains(io.MousePos);

                    drawList->AddRectFilled(barHandleLeft.Min, barHandleLeft.Max, (onLeft || seqCtx.SizingLBar) ? 0xFFAAAAAA : 0xFF666666, 6);
                    drawList->AddRectFilled(barHandleRight.Min, barHandleRight.Max, (onRight || seqCtx.SizingRBar) ? 0xFFAAAAAA : 0xFF666666, 6);

                    ImRect scrollBarThumb = new(scrollBarC, scrollBarD);

                    if (seqCtx.SizingRBar)
                    {
                        if (!io.MouseDown[0])
                        {
                            seqCtx.SizingRBar = false;
                        }
                        else
                        {
                            float barNewWidth = MathF.Max(barWidthInPixels + io.MouseDelta.X, seqCtx.MinBarWidth);
                            float barRatio = barNewWidth / barWidthInPixels;
                            seqCtx.FramePixelWidthTarget = seqCtx.FramePixelWidth /= barRatio;
                            int newVisibleFrameCount = (int)((canvasSize.X - legendWidth) / seqCtx.FramePixelWidthTarget);
                            int lastFrame = *firstFrame + newVisibleFrameCount;
                            if (lastFrame > sequence.GetFrameMax())
                            {
                                seqCtx.FramePixelWidthTarget = seqCtx.FramePixelWidth = (canvasSize.X - legendWidth) / (sequence.GetFrameMax() - *firstFrame);
                            }
                        }
                    }
                    else if (seqCtx.SizingLBar)
                    {
                        if (!io.MouseDown[0])
                        {
                            seqCtx.SizingLBar = false;
                        }
                        else
                        {
                            if (MathF.Abs(io.MouseDelta.X) > ImMath.FloatEpsilon)
                            {
                                float barNewWidth = MathF.Max(barWidthInPixels - io.MouseDelta.X, seqCtx.MinBarWidth);
                                float barRatio = barNewWidth / barWidthInPixels;
                                float previousFramePixelWidthTarget = seqCtx.FramePixelWidthTarget;
                                seqCtx.FramePixelWidthTarget = seqCtx.FramePixelWidth /= barRatio;
                                int newVisibleFrameCount = (int)(visibleFrameCount / barRatio);
                                int newFirstFrame = *firstFrame + newVisibleFrameCount - visibleFrameCount;
                                newFirstFrame = ImMath.Clamp(newFirstFrame, sequence.GetFrameMin(), Math.Max(sequence.GetFrameMax() - visibleFrameCount, sequence.GetFrameMin()));
                                if (newFirstFrame == *firstFrame)
                                {
                                    seqCtx.FramePixelWidth = seqCtx.FramePixelWidthTarget = previousFramePixelWidthTarget;
                                }
                                else
                                {
                                    *firstFrame = newFirstFrame;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (seqCtx.MovingScrollBar)
                        {
                            if (!io.MouseDown[0])
                            {
                                seqCtx.MovingScrollBar = false;
                            }
                            else
                            {
                                float framesPerPixelInBar = barWidthInPixels / visibleFrameCount;
                                *firstFrame = (int)((io.MousePos.X - seqCtx.PanningViewSource.X) / framesPerPixelInBar) - seqCtx.PanningViewFrame;
                                *firstFrame = ImMath.Clamp(*firstFrame, sequence.GetFrameMin(), Math.Max(sequence.GetFrameMax() - visibleFrameCount, sequence.GetFrameMin()));
                            }
                        }
                        else
                        {
                            if (scrollBarThumb.Contains(io.MousePos) && ImGui.IsMouseClicked(0) && firstFrame != null && !seqCtx.MovingCurrentFrame && seqCtx.MovingEntry == -1)
                            {
                                seqCtx.MovingScrollBar = true;
                                seqCtx.PanningViewSource = io.MousePos;
                                seqCtx.PanningViewFrame = -*firstFrame;
                            }
                            if (!seqCtx.SizingRBar && onRight && ImGui.IsMouseClicked(0))
                            {
                                seqCtx.SizingRBar = true;
                            }

                            if (!seqCtx.SizingLBar && onLeft && ImGui.IsMouseClicked(0))
                            {
                                seqCtx.SizingLBar = true;
                            }
                        }
                    }
                }
            }

            if (regionRect.Contains(io.MousePos))
            {
                bool overCustomDraw = false;
                for (int i = 0; i < customDraws.Size; ++i)
                {
                    ref var custom = ref customDraws.Data[i];
                    if (custom.CustomRect.Contains(io.MousePos))
                    {
                        overCustomDraw = true;
                    }
                }
                if (overCustomDraw)
                {
                }
                else
                {
#if false
            frameOverCursor = *firstFrame + (int)(visibleFrameCount * ((io.MousePos.X - (float)legendWidth - canvas_pos.X) / (canvas_size.X - legendWidth)));
            //frameOverCursor = Math.Max(Math.Min(*firstFrame - visibleFrameCount / 2, frameCount - visibleFrameCount), 0);

            /**firstFrame -= frameOverCursor;
            *firstFrame *= seqCtx.framePixelWidthTarget / framePixelWidth;
            *firstFrame += frameOverCursor;*/
            if (io.MouseWheel < -FloatEpsilon)
            {
               *firstFrame -= frameOverCursor;
               *firstFrame = int(*firstFrame * 1.1f);
               seqCtx.framePixelWidthTarget *= 0.9f;
               *firstFrame += frameOverCursor;
            }

            if (io.MouseWheel > FloatEpsilon)
            {
               *firstFrame -= frameOverCursor;
               *firstFrame = int(*firstFrame * 0.9f);
               seqCtx.framePixelWidthTarget *= 1.1f;
               *firstFrame += frameOverCursor;
            }
#endif
                }
            }

            if (expanded != null)
            {
                if (SequencerAddDelButton(drawList, new Vector2(canvasPos.X + 2, canvasPos.Y + 2), !*expanded))
                {
                    *expanded = !*expanded;
                }
            }

            if (delEntry != -1)
            {
                sequence.Del(delEntry);
                if (selectedEntry != null && (*selectedEntry == delEntry || *selectedEntry >= sequence.GetItemCount()))
                {
                    *selectedEntry = -1;
                }
            }

            if (dupEntry != -1)
            {
                sequence.Duplicate(dupEntry);
            }
            return ret;
        }
    }
}