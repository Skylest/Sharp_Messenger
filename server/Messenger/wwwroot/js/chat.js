$(document).ready(function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/chathub")
        .build();

    connection.start().then(function () {
        console.log('SignalR Started...')
        viewModel.channelsList();
        viewModel.usersList();
        setTimeout(function () {
            if (viewModel.channels().length > 0) {
                viewModel.joinChannel(viewModel.channels()[0]);
            }
        }, 250);
    }).catch(function (err) {
        return console.error(err);
    });

    connection.on("newMessage",function (messageView){
        var message = new Message(messageView.content, messageView.timestamp, messageView.from);
        viewModel.messages.push(message);
        $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);
    });

    connection.on("addUser", function(user){
        viewModel.userAdded(new User(user.Username, user.Nickname, user.CurrentChannel));
    });

    connection.on("removeUser", function(user){
        viewModel.userRemoved(user.Username);
    })

    connection.on("addChannel", function(channel){
        viewModel.channelAdded(new Channel(channel.id, channel.name));
    });

    connection.on("removeChannel", function(channel){
        viewModel.channelDeleted(channel.id);
    });

    connection.on("onChannelDeleted", function (message) {
        viewModel.serverInfoMessage(message);
        $("#errorAlert").removeClass("d-none").show().delay(5000).fadeOut(500);

        if (viewModel.channels().length - 1 == 0) {
            viewModel.joinedChannel("");
        }
        else {
            $("ul#channels_list li a")[0].click();
        }
    });

    connection.on("on_error", function(message){
        viewModel.serverInfoMessage(message);
        $("#errorAlert").removeClass("d_none").show().delay(5000).fadeOut(500);
        if(viewModel.channels().length - 1 == 0){
            viewModel.joinedChannel("");
        }else{
            $("ul#chanels_list li a")[0].click();
        }
    });

    function AppViewModel() {
        var self = this;

        self.message = ko.observable("");
        self.channels = ko.observableArray([]);
        self.users = ko.observableArray([]);
        self.messages = ko.observableArray([]);
        self.joinedChannel = ko.observable("");
        self.joinedChannelId = ko.observable("");
        self.serverInfoMessage = ko.observable("");
        self.myName = ko.observable("");

        self.onEnter = function (d, e) {
            if (e.keyCode === 13) {
                self.sendMessage();
            }
            return true;
        }

        self.sendMessage = function () {
            if (self.joinedChannel().length > 0 && self.message().length > 0)
                connection.invoke("Send", self.joinedChannel(), self.message());

            self.message("");
        }

        self.joinChannel = function (channel) {
            connection.invoke("Join", channel.Name()).then(function () {
                self.joinedChannel(channel.Name());
                self.joinedChannelId(channel.Id());
                self.usersList();
                self.messageHistory();
            });
        }

        self.channelsList = function () {
            connection.invoke("GetChannels").then(function (result) {
                self.channels.removeAll();
                for (var i = 0; i < result.length; i++) {
                    self.channels.push(new Channel(result[i].id, result[i].name));
                }
            });
        }

        self.usersList = function () {
            connection.invoke("GetUsers", self.joinedChannel()).then(function (result) {
                self.users.removeAll();
                for (var i = 0; i < result.length; i++) {
                    self.users.push(new User(result[i].Username,
                        result[i].Nickname,
                        result[i].CurrentChannel))
                }
            });
        }

        self.createChannel = function () {
            var name = $("#channelName").val();
            connection.invoke("CreateChannel", name);
        }

        self.deleteChannel = function () {
            connection.invoke("DeleteChannel", self.joinedChannel());
        }

        self.messageHistory = function () {
            connection.invoke("GetMessageHistory", self.joinedChannel()).then(function (result) {
                self.messages.removeAll();
                for (var i = 0; i < result.length; i++) {
                    self.messages.push(new Message(result[i].content,
                        result[i].timestamp,
                        result[i].from))
                }

                $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);
            });
        }

        self.channelAdded = function (channel) {
            self.channels.push(channel);
        }

        self.channelDeleted = function (id) {
            var temp;
            ko.utils.arrayForEach(self.channels(), function (channel) {
                if (channel.Id() == id)
                    temp = channel;
            });
            self.channels.remove(temp);
        }

        self.userAdded = function (user) {
            self.users.push(user);
        }

        self.userRemoved = function (id) {
            var temp;
            ko.utils.arrayForEach(self.users(), function (user) {
                if (user.userName() == id)
                    temp = user;
            });
            self.users.remove(temp);
        }

        self.uploadFile = function () {
            var form = document.getElementById("uploadForm");
            $.ajax({
                type: "POST",
                url: '/api/Upload',
                data: new FormData(form),
                contentType: false,
                processData: false,
                success: function () {
                    $("#UploadedFile").val("");
                },
                error: function (error) {
                    alert('Error: ' + error.responseText);
                }
            });
        }
    }

    function Channel(Id, Name){
        var self = this;
        self.Id = ko.observable(Id);
        self.Name = ko.observable(Name);
    }

    function User(Username, Nickname, CurrentChannel){
        var self = this;
        self.Username = ko.observable(Username);
        self.Nickname = ko.observable(Nickname);
        self.CurrentChannel = ko.observable(CurrentChannel);
    }

    function Message(Content, Timestamp, From){
        var self = this;
        self.Content = ko.observable(Content);
        self.Timestamp = ko.observable(Timestamp);
        self.From = ko.observable(From);
    }

    var viewModel = new AppViewModel();
    ko.applyBindings(viewModel);
});
