﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Discord_UWP.Gateway;
using Discord_UWP.SharedModels;
using Microsoft.Toolkit.Uwp.UI.Animations;

using Discord_UWP.LocalModels;
using Discord_UWP.Managers;
using Windows.UI;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.Effects;
using Windows.UI.Xaml.Hosting;
using System.Numerics;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=234238

namespace Discord_UWP.SubPages
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class UserProfile : Page
    {
        public UserProfile()
        {
            this.InitializeComponent();
            App.SubpageCloseHandler += App_SubpageCloseHandler;
        }

        private void App_SubpageCloseHandler(object sender, EventArgs e)
        {
            CloseButton_Click(null, null);
            App.SubpageCloseHandler -= App_SubpageCloseHandler;
            GatewayManager.Gateway.UserNoteUpdated -= Gateway_UserNoteUpdated;
            GatewayManager.Gateway.PresenceUpdated -= Gateway_PresenceUpdated;
            GatewayManager.Gateway.RelationShipAdded -= Gateway_RelationshipAdded;
            GatewayManager.Gateway.RelationShipUpdated -= Gateway_RelationshipUpdated;
            GatewayManager.Gateway.RelationShipRemoved -= Gateway_RelationshipRemoved;
        }

        private void NavAway_Completed(object sender, object e)
        {
            Frame.Visibility = Visibility.Collapsed;
        }

        private SharedModels.UserProfile profile;
        string userid;
        bool navFromFlyout = false;
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ConnectedAnimation imageAnimation =
        ConnectedAnimationService.GetForCurrentView().GetAnimation("avatar");
            if (App.navImageCache != null)
            {
                AvatarFull.ImageSource = App.navImageCache;
                App.navImageCache = null;
                navFromFlyout = true;
            }
            
            if (imageAnimation != null)
            {
                imageAnimation.TryStart(FullAvatar);
            }

            //Windows.UI.Color? color = (await App.getUserColor((e.Parameter as User)));

            //UserAccent = new SolidColorBrush(color.HasValue ? color.Value : (Windows.UI.Color)App.Current.Resources["BlurpleColor"]);

            bool loadviaRest = true;
            if (e.Parameter is User)
            {
                if (!(e.Parameter as User).Bot)
                {
                    profile = await RESTCalls.GetUserProfile((e.Parameter as User).Id);
                } else
                {
                    profile = new SharedModels.UserProfile();
                    profile.user = (User)e.Parameter;
                    grid.VerticalAlignment = VerticalAlignment.Center;
                }
                userid = profile.user.Id;
            }
            else if(e.Parameter is string)
            {
                userid = (e.Parameter as string);
                profile = await RESTCalls.GetUserProfile(e.Parameter as string); //TODO: Rig to App.Events (maybe, probably not actually)
                loadviaRest = false;
            }
            else
            {
                CloseButton_Click(null, null);
                return;
            }
            if (LocalState.Friends.ContainsKey(profile.user.Id))
            {
                profile.Friend = LocalState.Friends[profile.user.Id];
            }
            else
            {
                profile.Friend = null;
            }

            if (LocalState.PresenceDict.ContainsKey(userid))
            {
                if (LocalState.PresenceDict[userid].Status != null && LocalState.PresenceDict[userid].Status != "invisible")
                    rectangle.Fill = (SolidColorBrush)App.Current.Resources[LocalState.PresenceDict[userid].Status];
                else if (LocalState.PresenceDict[userid].Status == "invisible")
                    rectangle.Fill = (SolidColorBrush)App.Current.Resources["offline"];
            }
            else
            {
                rectangle.Fill = (SolidColorBrush)App.Current.Resources["offline"];
            }

            if (userid == LocalState.CurrentUser.Id) {
                AccountSettings.Visibility = Visibility.Visible;
            } else {
                AccountSettings.Visibility = Visibility.Collapsed;
            }


            if (LocalState.PresenceDict.ContainsKey(profile.user.Id))
            {
                if (LocalState.PresenceDict[profile.user.Id].Game != null)
                {
                    richPresence.GameContent = LocalState.PresenceDict[profile.user.Id].Game;
                }
                else
                    richPresence.Visibility = Visibility.Collapsed;
            }
            else
                richPresence.Visibility = Visibility.Collapsed;
            UpdateBorderColor();

            username.Text = profile.user.Username;
            username.Fade(1, 400);
            discriminator.Text = "#" + profile.user.Discriminator;
            discriminator.Fade(0.4f, 800);

            if (profile.Friend != null)
            {
                SwitchFriendValues(profile.Friend.Type);
            }
            else if (profile.user.Id == LocalState.CurrentUser.Id) { }
            else if (profile.user.Bot)
            {
                SendMessageLink.Visibility = Visibility.Visible;
                Block.Visibility = Visibility.Visible;
                loadviaRest = false;
                BotIndicator.Visibility = Visibility.Visible;
            }
            else
            {
                sendFriendRequest.Visibility = Visibility.Visible;
                SendMessageLink.Visibility = Visibility.Visible;
                Block.Visibility = Visibility.Visible;
                BotIndicator.Visibility = Visibility.Collapsed;
            }


            if (LocalState.Notes.ContainsKey(profile.user.Id))
                NoteBox.Text = LocalState.Notes[profile.user.Id];

            GatewayManager.Gateway.UserNoteUpdated += Gateway_UserNoteUpdated;
            GatewayManager.Gateway.PresenceUpdated += Gateway_PresenceUpdated;
            GatewayManager.Gateway.RelationShipAdded += Gateway_RelationshipAdded;
            GatewayManager.Gateway.RelationShipUpdated += Gateway_RelationshipUpdated;
            GatewayManager.Gateway.RelationShipRemoved += Gateway_RelationshipRemoved;

          //  BackgroundGrid.Blur(8, 0).Start();
            base.OnNavigatedTo(e);
            if(loadviaRest)
                profile = await RESTCalls.GetUserProfile(profile.user.Id);
            try
            {
                if (profile.connected_accounts != null)
                {
                    for (int i = 0; i < profile.connected_accounts.Count(); i++)
                    {
                        var element = profile.connected_accounts.ElementAt(i);
                        string themeExt = "";
                        if (element.Type.ToLower() == "steam")
                        {
                            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
                                themeExt = "_light";
                            else
                                themeExt = "_dark";
                        }
                        element.ImagePath = "/Assets/ConnectionLogos/" + element.Type.ToLower() + themeExt + ".png";
                        Connections.Items.Add(element);
                    }
                }

                if (profile.MutualGuilds != null)
                {
                    for (int i = 0; i < profile.MutualGuilds.Count(); i++)
                    {
                        var element = profile.MutualGuilds.ElementAt(i);
                        element.Name = LocalState.Guilds[element.Id].Raw.Name;
                        element.ImagePath = "https://discordapp.com/api/guilds/" + LocalState.Guilds[element.Id].Raw.Id + "/icons/" + LocalState.Guilds[element.Id].Raw.Icon + ".jpg";

                        if (element.Nick != null) element.NickVisibility = Visibility.Visible;
                        else element.NickVisibility = Visibility.Collapsed;

                        MutualGuilds.Items.Add(element);
                    }
                    if (MutualGuilds.Items.Count > 0)
                    {
                        commonServerPanel.Visibility = Visibility.Visible;
                        commonserverHeader.Fade(1, 300, 0).Start();
                    }
                }
            }
            catch
            {

            }
            

            switch (profile.user.Flags)
            {
                case 1:
                {
                    var img = new Image()
                    {
                        MaxHeight = 28,
                        Source = new BitmapImage(new Uri("ms-appx:///Assets/DiscordBadges/staff.png")),
                        Opacity=0
                    };
                    ToolTipService.SetToolTip(img,App.GetString("/Dialogs/FlagDiscordStaff").ToUpper());
                    BadgePanel.Children.Add(img);
                    img.Fade(1).Start();
                    break;
                }
                case 2:
                {
                    var img = new Image()
                    {
                        MaxHeight = 28,
                        Source = new BitmapImage(new Uri("ms-appx:///Assets/DiscordBadges/partner.png")),
                        Opacity = 0
                    };
                    ToolTipService.SetToolTip(img, App.GetString("/Dialogs/FlagDiscordPartner").ToUpper());
                    BadgePanel.Children.Add(img);
                    img.Fade(1).Start();
                    break;
                    }
                case 4:
                {
                    var img = new Image()
                    {
                        MaxHeight = 28,
                        Source = new BitmapImage(new Uri("ms-appx:///Assets/DiscordBadges/hypesquad.png")),
                        Opacity = 0
                    };
                    ToolTipService.SetToolTip(img, App.GetString("/Dialogs/FlagHypesquad"));
                    BadgePanel.Children.Add(img);
                    img.Fade(1).Start();
                    break;
                }
            }
            if(profile.user.Id == "109338686889476096")
            {
                ViewStats.Visibility = Visibility.Visible;
            }
            if (profile.PremiumSince.HasValue)
            {
                var img = new Image()
                {
                    MaxHeight = 28,
                    Source = new BitmapImage(new Uri("ms-appx:///Assets/DiscordBadges/nitro.png")),
                    Opacity = 0
                };
                ToolTipService.SetToolTip(img, App.GetString("/Main/PremiumMemberSince") + " " + Common.HumanizeDate(profile.PremiumSince.Value,null));
                BadgePanel.Children.Add(img);
                img.Fade(1.2f);
            }

            var imageurl = Common.AvatarUri(profile.user.Avatar, profile.user.Id);
            SetupComposition(imageurl);
            var image = new BitmapImage(imageurl);
            if (!navFromFlyout)
            {
                AvatarFull.ImageSource = image;
            }
          //  AvatarBlurred.Source = image;

            if (profile.user.Avatar != null)
            {
                AvatarBG.Fill = Common.GetSolidColorBrush("#00000000");
            } else
            {
                AvatarBG.Fill = Common.DiscriminatorColor(profile.user.Discriminator);
            }


            if (profile.user.Bot) return; 
            var relationships = await RESTCalls.GetUserRelationShips(profile.user.Id); //TODO: Rig to App.Events (maybe, probably not actually)
            int relationshipcount = relationships.Count();

            if (relationshipcount == 0) return;

            commonFriendPanel.Visibility = Visibility.Visible;
            commonfriendHeader.Fade(1, 300, 0).Start();
                for (int i = 0; i < relationshipcount; i++)
                {
                    var relationship = relationships.ElementAt(i);
                    relationship.Discriminator = "#" + relationship.Discriminator;
                    if (relationship.Avatar != null) relationship.ImagePath = "https://cdn.discordapp.com/avatars/" + relationship.Id + "/" + relationship.Avatar + ".png";
                    else relationship.ImagePath = "ms-appx:///Assets/DiscordIcon.png";

                    MutualFriends.Items.Add(relationship);
                }

        }
        private void UpdateBorderColor()
        {
            if (richPresence.GameContent != null)
            {
                richPresence.Visibility = Visibility.Visible;
                SolidColorBrush color = (SolidColorBrush)Application.Current.Resources["Blurple"];
                switch (richPresence.GameContent.Type)
                {
                    case 1:
                        {
                            //streaming
                            color = new SolidColorBrush(Color.FromArgb(255, 100, 65, 164));
                            break;
                        }
                    case 2:
                        {
                            //spotify
                            color = new SolidColorBrush(Color.FromArgb(255, 30, 215, 96));
                            break;
                        }
                }
                if (LocalState.PresenceDict[profile.user.Id].Game != null && LocalState.PresenceDict[profile.user.Id].Game.ApplicationId == "438122941302046720")
                {
                    //xbox
                    color = new SolidColorBrush(Color.FromArgb(255, 16, 124, 16));
                }
                PresenceColor.Fill = color;
                border.BorderBrush = color;
            }
        }

        private async void Gateway_PresenceUpdated(object sender, GatewayEventArgs<Presence> e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if (e.EventData.User.Id == profile.user.Id)
                        {
                            if (e.EventData.Game != null)
                            {
                                var game = e.EventData.Game;
                                richPresence.GameContent = game;
                                richPresence.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                richPresence.Visibility = Visibility.Collapsed;
                            }

                            if (e.EventData.Status != null && e.EventData.Status != "invisible")
                                rectangle.Fill = (SolidColorBrush)App.Current.Resources[e.EventData.Status];
                            else if (e.EventData.Status == "invisible")
                                rectangle.Fill = (SolidColorBrush)App.Current.Resources["offline"];
                            UpdateBorderColor();
                        }
                    });
        }

        private async void Gateway_RelationshipUpdated(object sender, GatewayEventArgs<Friend> gatewayEventArgs)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if (gatewayEventArgs.EventData.user.Id == profile.user.Id)
                            SwitchFriendValues(gatewayEventArgs.EventData.Type);
                    });
        }

        private async void Gateway_RelationshipAdded(object sender, GatewayEventArgs<Friend> gatewayEventArgs)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if (gatewayEventArgs.EventData.user.Id == profile.user.Id)
                            SwitchFriendValues(gatewayEventArgs.EventData.Type);
                    });
        }

        private void Gateway_RelationshipRemoved(object sender, GatewayEventArgs<Friend> e)
        {
            if (e.EventData.Id == profile.user.Id)
                SwitchFriendValues(0);
        }

        private async void SwitchFriendValues(int type)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {

                RemoveFriendLink.Visibility = Visibility.Collapsed;
                Block.Visibility = Visibility.Collapsed;
                SendMessageLink.Visibility = Visibility.Visible;
                pendingFriend.Visibility = Visibility.Collapsed;
                acceptFriend.Visibility = Visibility.Collapsed;
                sendFriendRequest.Visibility = Visibility.Collapsed;
                switch (type)
                {
                    case 0:
                        //No relationship
                        Block.Visibility = Visibility.Visible;
                        sendFriendRequest.Visibility = Visibility.Visible;
                        break;
                    case 1:
                        //Friend
                        RemoveFriendLink.Visibility = Visibility.Visible;
                        SendMessageLink.Visibility = Visibility.Visible;
                        Block.Visibility = Visibility.Visible;
                        break;
                    case 2:
                        //Blocked
                        Unblock.Visibility = Visibility.Visible;
                        SendMessageLink.Visibility = Visibility.Visible;
                        break;
                    case 3:
                        //Pending incoming friend request
                        acceptFriend.Visibility = Visibility.Visible;
                        SendMessageLink.Visibility = Visibility.Visible;
                        Block.Visibility = Visibility.Visible;
                        break;
                    case 4:
                        //Pending outgoing friend request
                        pendingFriend.Visibility = Visibility.Visible;
                        SendMessageLink.Visibility = Visibility.Visible;
                        Block.Visibility = Visibility.Visible;
                        break;
                }
            });
        }
        SpriteVisual _imageVisual;
        private void SetupComposition(Uri imageURL)
        {
            try
            {
                CompositionSurfaceBrush _imageBrush;

                Compositor _compositor = Window.Current.Compositor;
                _imageBrush = _compositor.CreateSurfaceBrush();
                _imageBrush.Stretch = CompositionStretch.UniformToFill;


                LoadedImageSurface _loadedSurface = LoadedImageSurface.StartLoadFromUri(imageURL);
                _imageBrush.Surface = _loadedSurface;

                var saturationEffect = new SaturationEffect
                {
                    Saturation = 0.0f,
                    Source = new CompositionEffectSourceParameter("image")
                };
                var effectFactory = _compositor.CreateEffectFactory(saturationEffect);
                var effectBrush = effectFactory.CreateBrush();
                effectBrush.SetSourceParameter("image", _imageBrush);

                var blurEffect = new GaussianBlurEffect
                {
                    BlurAmount = 8,
                    Source = new CompositionEffectSourceParameter("image")
                };
                var effectFactory2 = _compositor.CreateEffectFactory(blurEffect);
                var effectBrush2 = effectFactory2.CreateBrush();
                effectBrush2.SetSourceParameter("image", effectBrush);

                _imageVisual = _compositor.CreateSpriteVisual();
                _imageVisual.Brush = effectBrush2;
                _imageVisual.Size = new Vector2(Convert.ToSingle(AvatarContainer.ActualWidth), Convert.ToSingle(AvatarContainer.ActualHeight));

                
                if (ParallaxScroll != null)
                {
                    CompositionPropertySet scrollerViewerManipulation = ElementCompositionPreview.GetScrollViewerManipulationPropertySet(ParallaxScroll);
                    ExpressionAnimation expression = _compositor.CreateExpressionAnimation("ScrollManipulation.Translation.Y * 0.4");
                    expression.SetReferenceParameter("ScrollManipulation", scrollerViewerManipulation);
                    _imageVisual.StartAnimation("Offset.Y", expression);

                    var avatarvisual = ElementCompositionPreview.GetElementVisual(FullAvatar);

                    ExpressionAnimation expression2 = _compositor.CreateExpressionAnimation("ScrollManipulation.Translation.Y * 0.14");
                    expression2.SetReferenceParameter("ScrollManipulation", scrollerViewerManipulation);
                    avatarvisual.StartAnimation("Offset.Y", expression2);

                    var usernameVisual = ElementCompositionPreview.GetElementVisual(usernamestacker);
                    ElementCompositionPreview.SetIsTranslationEnabled(usernamestacker, true);
                    ExpressionAnimation expression3 = _compositor.CreateExpressionAnimation("ScrollManipulation.Translation.Y * 0.06");
                    expression3.SetReferenceParameter("ScrollManipulation", scrollerViewerManipulation);
                    usernameVisual.StartAnimation("Translation.Y", expression3);
                }

                BackgroundGrid.Clip = new RectangleGeometry() { Rect = new Rect(new Point(0, 0), new Point(AvatarContainer.ActualWidth, AvatarContainer.ActualHeight)) };
                ElementCompositionPreview.SetElementChildVisual(AvatarContainer, _imageVisual);
            }
            catch
            {
                //Fuck this shit
            }

        }
        private async void Gateway_UserNoteUpdated(object sender, GatewayEventArgs<Gateway.DownstreamEvents.UserNote> e)
        {
            if (e.EventData.UserId == profile.user.Id)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                    () =>
                    {
                        NoteBox.Text = e.EventData.Note;
                    });
            }
        }

        private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            CloseButton_Click(null, null);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            scale.CenterY = this.ActualHeight / 2;
            scale.CenterX = this.ActualWidth / 2;
            GatewayManager.Gateway.UserNoteUpdated -= Gateway_UserNoteUpdated;
            GatewayManager.Gateway.PresenceUpdated -= Gateway_PresenceUpdated;
            GatewayManager.Gateway.RelationShipAdded -= Gateway_RelationshipAdded;
            GatewayManager.Gateway.RelationShipUpdated -= Gateway_RelationshipUpdated;
            GatewayManager.Gateway.RelationShipRemoved -= Gateway_RelationshipRemoved;
            NavAway.Begin();
            App.SubpageClosed();
        }

        private async void NoteBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var userid = profile.user.Id;
            var note = NoteBox.Text.Trim();
            if (LocalState.Notes.ContainsKey(userid) && note == LocalState.Notes[userid].Trim())
                return;
            if (!LocalState.Notes.ContainsKey(userid) && string.IsNullOrEmpty(note))
                return;
            await RESTCalls.AddNote(profile.user.Id, NoteBox.Text); //TODO: Rig to App.Events
        }

        private void FadeIn_ImageOpened(object sender, RoutedEventArgs e)
        {
            (sender as Windows.UI.Xaml.Controls.Image).Fade(0.2f).Start();
        }

        private void AvatarFull_OnImageOpened(object sender, RoutedEventArgs e)
        {
            Avatar.Fade(1).Start();
        }

        private async void Connections_OnItemClick(object sender, ItemClickEventArgs e)
        {
            var item = (ConnectedAccount) e.ClickedItem;
            if(item.Id == null) return;
            string url = null;
            switch (item.Type)
            {
                case "steam": url = "https://steamcommunity.com/profiles/" + item.Id;
                    break;
                case "skype": url = "skype:" + item.Id + "?userinfo";
                    break;
                case "reddit": url = "https://www.reddit.com/u/" + item.Name;
                    break;
                case "facebook": url = "https://www.facebook.com/" + item.Id;
                    break;
                case "patreon": url = "https://www.patreon.com/" + item.Id;
                    break;
                case "twitter": url = "https://www.twitter.com/" + item.Name;
                    break;
                case "twitch": url = "https://www.twitch.tv/" + item.Id;
                    break;
                case "youtube": url = "https://www.youtube.com/channel/" + item.Id;
                    break;
                case "leagueoflegends": url = null;
                    break;
                default: url = null;
                    break;

            }
            if(url != null) await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
        }

        private void MutualGuilds_ItemClick(object sender, ItemClickEventArgs e)
        {
            //TODO: Open guild
        }

        private async void SendFriendRequest(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                await RESTCalls.SendFriendRequest(profile.user.Id); //TODO: Rig to App.Events
            });

        }

        private async void RemoveFriend(object sender, RoutedEventArgs e)
        {
            await Task.Run(async () =>
            {
                await RESTCalls.RemoveFriend(profile.user.Id); //TODO: Rig to App.Events
            });
        }

        private async void SendMessageLink_Click(object sender, RoutedEventArgs e)
        {
            CloseButton_Click(null, null);
            string channelid = null;
            foreach (var dm in LocalState.DMs)
                if (dm.Value.Type == 1 && dm.Value.Users.FirstOrDefault()?.Id == (sender as MenuFlyoutItem).Tag.ToString())
                    channelid = dm.Value.Id;
            if (channelid == null)
                channelid = (await RESTCalls.CreateDM(new API.User.Models.CreateDM() { Recipients = new List<string>() { (sender as MenuFlyoutItem).Tag.ToString() }.AsEnumerable() })).Id;
            if (string.IsNullOrEmpty(channelid)) return;
            App.SelectGuildChannel("@me", channelid);
        }

        private void Block_Click(object sender, RoutedEventArgs e)
        {
            App.BlockUser(userid);
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SubPages.UserProfileCU));
        }

        private void AvatarContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_imageVisual != null)
            {
                _imageVisual.Size = new Vector2(Convert.ToSingle(AvatarContainer.ActualWidth), Convert.ToSingle(AvatarContainer.ActualHeight));
                BackgroundGrid.Clip = new RectangleGeometry() { Rect = new Rect(new Point(0, 0), new Point(AvatarContainer.ActualWidth, AvatarContainer.ActualHeight)) };
            }
        }

        private void AccountSettings_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(SubPages.UserProfileCU));
        }

        private async void ViewStats_Click(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("https://www.carbonitex.net/discord/server?s=" + App.CurrentGuildId));
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public BooleanToVisibilityConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && (bool)value)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value is Visibility && (Visibility)value == Visibility.Visible);
        }
    }

    public class BooleanToVisibilityConverterInverse : IValueConverter
    {
        public BooleanToVisibilityConverterInverse()
        {
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && (bool)value)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value is Visibility && (Visibility)value == Visibility.Collapsed);
        }
    }
}
